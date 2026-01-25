using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Update;

public record UpdateCustomerCommand(
    Guid Id,
    string FullName,
    string Phone,
    string? Address = null,
    string? ExternalReference = null) : IRequest<Result>;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantProvider _tenantProvider;

    public UpdateCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    {
        _customerRepository = customerRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure("Tenant context missing.");

        var customer = await _customerRepository.GetByIdAsync(request.Id);

        if (customer == null || customer.TenantId != tenantId.Value)
        {
            return Result.Failure("Customer not found.");
        }

        // Check if phone changed and is already taken by another customer in the same tenant
        if (customer.Phone != request.Phone)
        {
            var existing = await _customerRepository.GetByPhoneAsync(tenantId.Value, request.Phone);
            if (existing != null)
            {
                return Result.Failure("Another customer already has this phone number.");
            }
        }

        customer.UpdateDetails(request.FullName, request.Phone, request.Address, request.ExternalReference);

        await _customerRepository.UpdateAsync(customer);

        return Result.Success();
    }
}

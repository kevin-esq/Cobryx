using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Update;

public record UpdateCustomerCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    Address? Address = null,
    IdentityDocument? Document = null) : IRequest<Result>;

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

        if (customer.Phone != request.Phone)
        {
            var existing = await _customerRepository.GetByPhoneAsync(tenantId.Value, request.Phone);
            if (existing != null)
            {
                return Result.Failure("Another customer already has this phone number.");
            }
        }

        customer.UpdateDetails(
            request.FirstName, 
            request.LastName, 
            request.Phone, 
            request.Address, 
            request.Document);

        await _customerRepository.UpdateAsync(customer);

        return Result.Success();
    }
}

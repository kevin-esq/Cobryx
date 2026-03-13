using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Update;

public record UpdateCustomerCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
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
        if (!tenantId.HasValue) throw new TenantContextMissingException();

        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null || customer.TenantId != tenantId.Value)
        {
            throw new CustomerNotFoundException(request.Id);
        }

        if (customer.Phone != request.Phone)
        {
            var existing = await _customerRepository.GetByPhoneAsync(tenantId.Value, request.Phone, cancellationToken);
            if (existing != null)
            {
                throw new DuplicateCustomerException();
            }
        }

        customer.UpdateDetails(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.Address,
            request.Document);

        await _customerRepository.UpdateAsync(customer, cancellationToken);

        return Result.Success();
    }
}

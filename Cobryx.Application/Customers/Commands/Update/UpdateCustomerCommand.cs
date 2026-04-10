using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Update;

[TenantScoped]
public record UpdateCustomerCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    Address? Address = null,
    IdentityDocument? Document = null) : IRequest<Result>, IRequiresTenant;

public class UpdateCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    : IRequestHandler<UpdateCustomerCommand, Result>
{
    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new TenantContextMissingException();

        Customer? customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null || customer.TenantId != tenantId.Value)
        {
            throw new CustomerNotFoundException(request.Id);
        }

        if (customer.Phone != request.Phone)
        {
            Customer? existing = await customerRepository.GetByPhoneAsync(tenantId.Value, request.Phone, cancellationToken);
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

        await customerRepository.UpdateAsync(customer, cancellationToken);

        return Result.Success();
    }
}

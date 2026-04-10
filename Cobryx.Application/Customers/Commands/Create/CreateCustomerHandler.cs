using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider) : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);

        Cobryx.Domain.Lending.Customer? existing = await customerRepository.GetByPhoneAsync(tenantId.Value, request.Phone, cancellationToken);
        if (existing != null)
        {
            throw new DuplicateCustomerException();
        }

        var customer = new Cobryx.Domain.Lending.Customer(
            tenantId.Value,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.Address,
            request.Document
        );

        await customerRepository.AddAsync(customer, cancellationToken);

        return Result.Success(customer.Id);
    }
}

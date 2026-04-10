using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Delete;

[TenantScoped]
public record DeleteCustomerCommand(Guid Id) : IRequest<Result>, IRequiresTenant;

public class DeleteCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    : IRequestHandler<DeleteCustomerCommand, Result>
{
    public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new TenantContextMissingException();

        Customer? customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null || customer.TenantId != tenantId.Value)
        {
            throw new CustomerNotFoundException(request.Id);
        }

        await customerRepository.DeleteAsync(customer.Id, cancellationToken);

        return Result.Success();
    }
}

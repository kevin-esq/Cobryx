using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Customers.Common;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customers.Queries.GetCustomerById;

[TenantScoped]
public record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>, IRequiresTenant;

public class GetCustomerByIdHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new TenantContextMissingException();

        Customer? customer = await customerRepository.Query()
            .AsNoTracking()
            .Include(c => c.LendingInstruments)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId.Value, cancellationToken);

        if (customer == null)
        {
            throw new CustomerNotFoundException(request.Id);
        }

        return Result.Success(new CustomerDto(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.FullName,
            customer.Phone,
            customer.Address,
            customer.Document,
            customer.LendingInstruments.OfType<Credit>().Count(c => c.Status == CreditStatus.Active),
            customer.CreatedAt));
    }
}

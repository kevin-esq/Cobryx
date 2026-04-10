using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Application.Customers.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customers.Queries.GetCustomers;

[TenantScoped]
public record GetCustomersQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<CustomerDto>>>, IRequiresTenant;

public class GetCustomersHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider) : IRequestHandler<GetCustomersQuery, Result<PaginatedList<CustomerDto>>>
{
    public async Task<Result<PaginatedList<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<PaginatedList<CustomerDto>>(DomainErrorCode.Tenant.ContextMissing);

        IQueryable<Customer> query = customerRepository.Query()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Phone.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        List<CustomerDto> items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto(
                c.Id,
                c.FirstName,
                c.LastName,
                c.FullName,
                c.Phone,
                c.Address,
                c.Document,
                c.LendingInstruments.OfType<Credit>().Count(cr => cr.Status == CreditStatus.Active),
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<CustomerDto>(
            items,
            totalCount,
            request.Page,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }
}

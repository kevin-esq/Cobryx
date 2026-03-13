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

public record GetCustomersQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<CustomerDto>>>;

public class GetCustomersHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider) : IRequestHandler<GetCustomersQuery, Result<PaginatedList<CustomerDto>>>
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    public async Task<Result<PaginatedList<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<PaginatedList<CustomerDto>>(DomainErrorCode.Tenant.ContextMissing);

        var query = _customerRepository.Query()
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

        var items = await query
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

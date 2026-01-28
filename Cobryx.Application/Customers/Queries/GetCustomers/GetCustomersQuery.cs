using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Customers.Common;
using Cobryx.Application.Common.Models;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Entities;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customers.Queries.GetCustomers;

public record GetCustomersQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<CustomerDto>>>;

public class GetCustomersHandler : IRequestHandler<GetCustomersQuery, Result<PaginatedList<CustomerDto>>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetCustomersHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    {
        _customerRepository = customerRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PaginatedList<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<PaginatedList<CustomerDto>>("Tenant context missing.");

        var query = _customerRepository.Query()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(search) ||
                c.LastName.ToLower().Contains(search) ||
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
                c.Credits.Count(cr => cr.Status == CreditStatus.Active),
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<CustomerDto>(
            items,
            totalCount,
            request.Page,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }
}

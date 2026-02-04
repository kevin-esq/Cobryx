using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Credits.Common;
using Cobryx.Application.Common.Models;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Credits.Queries.GetCredits;

public record GetCreditsQuery(
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<CreditDto>>>;

public class GetCreditsHandler : IRequestHandler<GetCreditsQuery, Result<PaginatedList<CreditDto>>>
{
    private readonly ICreditRepository _creditRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetCreditsHandler(ICreditRepository creditRepository, ITenantProvider tenantProvider)
    {
        _creditRepository = creditRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PaginatedList<CreditDto>>> Handle(GetCreditsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<PaginatedList<CreditDto>>("Tenant context missing.");

        var query = _creditRepository.Query()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId.Value);

        if (request.CustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId == request.CustomerId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CreditDto(
                c.Id,
                c.CustomerId,
                $"{c.Customer.FirstName} {c.Customer.LastName}",
                c.Principal.Amount,
                c.Principal.Currency,
                c.InterestRate,
                c.InstallmentsCount,
                c.Status,
                c.StartDate,
                c.Installments.Sum(i => i.PrincipalPaid.Amount + i.InterestPaid.Amount + i.LateInterestPaid.Amount),
                c.Installments.Sum(i => i.PrincipalPart.Amount + i.InterestPart.Amount + i.LateInterestAmount.Amount) -
                c.Installments.Sum(i => i.PrincipalPaid.Amount + i.InterestPaid.Amount + i.LateInterestPaid.Amount)
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedList<CreditDto>(
            items,
            totalCount,
            request.Page,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }
}

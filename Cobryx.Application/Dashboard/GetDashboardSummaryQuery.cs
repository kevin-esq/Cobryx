using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Dashboard;

public record DashboardSummaryDto
{
    public int ActiveLoansCount { get; init; }
    public decimal TotalPrincipalBalance { get; init; }
    public int OverdueLoansCount { get; init; }
    public decimal TotalArrearsAmount { get; init; }
    public int ActiveCustomersCount { get; init; }
    public decimal CollectionsLast30Days { get; init; }
    public List<RiskDistributionDto> RiskDistribution { get; init; } = new();
}

public record RiskDistributionDto(string Status, int Count);

public record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public GetDashboardSummaryQueryHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var dbContext = (DbContext)_unitOfWork;

        var loans = await dbContext.Set<Domain.Entities.Lending.Loan>()
            .Where(l => l.TenantId == tenantId && !l.IsDeleted && l.Status != Domain.Entities.Lending.Enums.LoanStatus.Closed)
            .ToListAsync(cancellationToken);

        var customersCount = await dbContext.Set<Domain.Entities.Customer>()
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .CountAsync(cancellationToken);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var collections = await dbContext.Set<Domain.Entities.Payments.Payment>()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.CreatedAt >= thirtyDaysAgo)
            .SumAsync(p => p.Amount.Amount, cancellationToken);

        var summary = new DashboardSummaryDto
        {
            ActiveLoansCount = loans.Count(l => l.Status == Domain.Entities.Lending.Enums.LoanStatus.Active),
            TotalPrincipalBalance = loans.Sum(l => l.CurrentPrincipalBalance),
            OverdueLoansCount = loans.Count(l => l.DaysInArrears > 0),
            TotalArrearsAmount = loans.Sum(l => l.CurrentInterestBalance + l.CurrentLateFeeBalance),
            ActiveCustomersCount = customersCount,
            CollectionsLast30Days = collections,
            RiskDistribution = loans
                .GroupBy(l => l.RiskStatus)
                .Select(g => new RiskDistributionDto(g.Key.ToString(), g.Count()))
                .ToList()
        };

        return Result.Success(summary);
    }
}

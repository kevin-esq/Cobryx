using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Dashboard;

/// <summary>
/// High-level financial and operational snapshot for the tenant dashboard.
/// </summary>
public record DashboardSummaryDto
{
    /// <summary>Total number of loans currently in active/performing status.</summary>
    public int ActiveLoansCount { get; init; }

    /// <summary>The aggregated outstanding principal across all active loans.</summary>
    public decimal TotalPrincipalBalance { get; init; }

    /// <summary>Number of loans that have at least one overdue installment.</summary>
    public int OverdueLoansCount { get; init; }

    /// <summary>Total amount of principal, interest, and fees currently past due.</summary>
    public decimal TotalArrearsAmount { get; init; }

    /// <summary>Total number of customers registered for the tenant.</summary>
    public int ActiveCustomersCount { get; init; }

    /// <summary>Total collections (payments received) in the last 30 calendar days.</summary>
    public decimal CollectionsLast30Days { get; init; }

    /// <summary>Collection Efficiency percentage (Collected vs Expected in last 30 days).</summary>
    public decimal CollectionEfficiency { get; init; }

    /// <summary>Portfolio Yield percentage (Interest Income vs Principal Disbursed).</summary>
    public decimal PortfolioYield { get; init; }

    /// <summary>Overdue Risk percentage (Percentage of loans currently past due).</summary>
    public decimal OverdueRisk { get; init; }

    /// <summary>Flag to indicate if a 'Getting Started' empty state should be shown.</summary>
    public bool ShowGettingStarted { get; init; }

    /// <summary>Flag to indicate if the displayed data is from a demonstration seed.</summary>
    public bool IsDemoData { get; init; }

    /// <summary>Psychographic summary signals to create urgency or highlight success.</summary>
    public List<ImpactSignalDto> ImpactSignals { get; init; } = new();

    /// <summary>Breakdown of loan counts by their internal risk/performance status.</summary>
    public List<RiskDistributionDto> RiskDistribution { get; init; } = new();
}

/// <summary>
/// Represents a semantic signal about the business impact of a metric.
/// </summary>
public record ImpactSignalDto(
    string Code,
    string Level // Success, Info, Warning, Critical
);

/// <summary>
/// Represents a count of loans categorized by a specific risk or status label.
/// </summary>
public record RiskDistributionDto(
    string Status,
    int Count
)
{
    /// <summary>The status label (e.g., Performing, Watchlist, Default).</summary>
    /// <example>Performing</example>
    public string Status { get; init; } = Status;

    /// <summary>Number of loans falling into this category.</summary>
    /// <example>112</example>
    public int Count { get; init; } = Count;
}

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

        // 1. Fetch Core Data
        var loans = await dbContext.Set<Domain.Entities.Lending.Loan>()
            .Where(l => l.TenantId == tenantId && !l.IsDeleted && l.Status != Domain.Entities.Lending.Enums.LoanStatus.Closed)
            .ToListAsync(cancellationToken);

        var customersCount = await dbContext.Set<Domain.Entities.Customer>()
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .CountAsync(cancellationToken);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var collectionsLast30d = await dbContext.Set<Domain.Entities.Payments.Payment>()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.PaymentDate >= thirtyDaysAgo && p.Status == Domain.Enums.PaymentStatus.Completed)
            .SumAsync(p => p.Amount.Amount, cancellationToken);

        var expectedCollectionsLast30d = await dbContext.Set<Domain.Entities.Lending.Installment>()
            .Where(i => i.Loan.TenantId == tenantId && !i.IsDeleted && i.DueDate >= thirtyDaysAgo && i.DueDate <= DateTime.UtcNow)
            .SumAsync(i => i.TotalAmount, cancellationToken);

        var totalPrincipalDisbursed = await dbContext.Set<Domain.Entities.Lending.Loan>()
            .Where(l => l.TenantId == tenantId && !l.IsDeleted)
            .SumAsync(l => l.OriginalPrincipal, cancellationToken);

        var activeLoans = loans.Where(l => l.Status == Domain.Entities.Lending.Enums.LoanStatus.Active).ToList();
        var totalPrincipalBalance = loans.Sum(l => l.CurrentPrincipalBalance);
        var overdueCount = loans.Count(l => l.DaysInArrears > 0);

        var overdueRisk = activeLoans.Any()
            ? (decimal)overdueCount / activeLoans.Count * 100
            : 0;

        var totalArrears = loans.Sum(l => l.CurrentInterestBalance + l.CurrentLateFeeBalance);
        var collectionEfficiency = expectedCollectionsLast30d > 0
            ? (collectionsLast30d / expectedCollectionsLast30d) * 100
            : 100;

        var portfolioYield = totalPrincipalDisbursed > 0
            ? (loans.Sum(l => l.CurrentInterestBalance) / totalPrincipalDisbursed) * 100
            : 0;

        var signals = new List<ImpactSignalDto>();

        if (overdueRisk > 15)
            signals.Add(new ImpactSignalDto("CASHFLOW_AT_RISK", "Critical"));
        else if (overdueRisk > 5)
            signals.Add(new ImpactSignalDto("COLLECTIONS_LAGGING", "Warning"));

        if (portfolioYield > 10)
            signals.Add(new ImpactSignalDto("YIELD_EXCELLENT", "Success"));
        else if (portfolioYield < 3 && totalPrincipalDisbursed > 0)
            signals.Add(new ImpactSignalDto("YIELD_UNREALIZED", "Info"));

        if (collectionEfficiency < 80)
            signals.Add(new ImpactSignalDto("EFFICIENCY_LOW", "Warning"));

        var summary = new DashboardSummaryDto
        {
            ActiveLoansCount = activeLoans.Count,
            TotalPrincipalBalance = totalPrincipalBalance,
            OverdueLoansCount = overdueCount,
            TotalArrearsAmount = totalArrears,
            ActiveCustomersCount = customersCount,
            CollectionsLast30Days = collectionsLast30d,
            CollectionEfficiency = Math.Round(collectionEfficiency, 2),
            PortfolioYield = Math.Round(portfolioYield, 2),
            OverdueRisk = Math.Round(overdueRisk, 2),
            ShowGettingStarted = !loans.Any(),
            IsDemoData = loans.Any(l => l.IsDemo),
            ImpactSignals = signals,
            RiskDistribution = loans
                .GroupBy(l => l.RiskStatus)
                .Select(g => new RiskDistributionDto(g.Key.ToString(), g.Count()))
                .ToList()
        };

        return Result.Success(summary);
    }
}

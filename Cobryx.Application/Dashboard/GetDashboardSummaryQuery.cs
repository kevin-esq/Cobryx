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
    /// <example>124</example>
    public int ActiveLoansCount { get; init; }

    /// <summary>The aggregated outstanding principal across all active loans.</summary>
    /// <example>2540000.50</example>
    public decimal TotalPrincipalBalance { get; init; }

    /// <summary>Number of loans that have at least one overdue installment.</summary>
    /// <example>12</example>
    public int OverdueLoansCount { get; init; }

    /// <summary>Total amount of principal, interest, and fees currently past due.</summary>
    /// <example>45200.00</example>
    public decimal TotalArrearsAmount { get; init; }

    /// <summary>Total number of customers registered for the tenant.</summary>
    /// <example>382</example>
    public int ActiveCustomersCount { get; init; }

    /// <summary>Total collections (payments received) in the last 30 calendar days.</summary>
    /// <example>185200.00</example>
    public decimal CollectionsLast30Days { get; init; }

    /// <summary>Breakdown of loan counts by their internal risk/performance status.</summary>
    public List<RiskDistributionDto> RiskDistribution { get; init; } = new();
}

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

        var loans = await dbContext.Set<Domain.Entities.Lending.Loan>()
            .Where(l => l.TenantId == tenantId && !l.IsDeleted && l.Status != Domain.Entities.Lending.Enums.LoanStatus.Closed)
            .ToListAsync(cancellationToken);

        var customersCount = await dbContext.Set<Domain.Entities.Customer>()
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .CountAsync(cancellationToken);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var collections = await dbContext.Set<Domain.Entities.Payments.Payment>()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.PaymentDate >= thirtyDaysAgo && p.Status == Domain.Enums.PaymentStatus.Completed)
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

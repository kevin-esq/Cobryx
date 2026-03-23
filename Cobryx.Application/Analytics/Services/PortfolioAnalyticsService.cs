using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Payments.Enums;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Analytics.Services;

public interface IPortfolioAnalyticsService
{
    public Task CalculateAllNightlyMetricsAsync(DateTime date, CancellationToken ct = default);
}

public class PortfolioAnalyticsService : IPortfolioAnalyticsService
{
    private readonly ICobryxDbContext _db;

    public PortfolioAnalyticsService(ICobryxDbContext db)
    {
        _db = db;
    }

    public async Task CalculateAllNightlyMetricsAsync(DateTime date, CancellationToken ct = default)
    {
        // Multi-Tenant Batch Aggregation using Window SQL Functions for O(1) query scalability
        var batchQuery = @"
WITH latest_snapshots AS (
    SELECT
        ""TenantId"",
        ""LoanId"",
        ""PrincipalBalance"",
        ""InterestBalance"",
        ""LateFeeBalance"",
        ""DaysPastDue"",
        ROW_NUMBER() OVER (
            PARTITION BY ""LoanId""
            ORDER BY ""RecordedAt"" DESC
        ) AS rn
    FROM ""LoanBalanceSnapshots""
),
current_loans AS (
    SELECT
        ""TenantId"",
        (""PrincipalBalance"" + ""InterestBalance"" + ""LateFeeBalance"") AS outstanding,
        ""PrincipalBalance"",
        ""InterestBalance"",
        ""LateFeeBalance"",
        ""DaysPastDue""
    FROM latest_snapshots
    WHERE rn = 1
      AND (""PrincipalBalance"" + ""InterestBalance"" + ""LateFeeBalance"") > 0
)
SELECT
    ""TenantId"",
    CAST(COUNT(*) AS int) AS ""TotalLoans"",
    SUM(outstanding) AS ""TotalOutstanding"",
    SUM(""PrincipalBalance"") AS ""TotalPrincipal"",
    SUM(""InterestBalance"") AS ""TotalInterest"",
    SUM(""LateFeeBalance"") AS ""TotalLateFees"",
    SUM(
        CASE WHEN ""DaysPastDue"" >= 90
        THEN outstanding ELSE 0 END
    ) AS ""NplOutstanding"",
    SUM(
        CASE WHEN ""DaysPastDue"" <= 30
        THEN outstanding ELSE 0 END
    ) AS ""Bucket0To30"",
    SUM(
        CASE WHEN ""DaysPastDue"" BETWEEN 31 AND 60
        THEN outstanding ELSE 0 END
    ) AS ""Bucket31To60"",
    SUM(
        CASE WHEN ""DaysPastDue"" BETWEEN 61 AND 90
        THEN outstanding ELSE 0 END
    ) AS ""Bucket61To90"",
    SUM(
        CASE WHEN ""DaysPastDue"" > 90
        THEN outstanding ELSE 0 END
    ) AS ""Bucket90Plus""
FROM current_loans
GROUP BY ""TenantId"";";

        var aggregates = await _db.TenantPortfolioAggregates
            .FromSqlRaw(batchQuery)
            .ToListAsync(ct);

        var startOfMonth = new DateTime(date.Year, date.Month, 1);
        var startOfYear = new DateTime(date.Year, 1, 1);

        var revenueMtdByTenant = await _db.LoanPaymentAllocations
            .Where(x => x.AllocationDate >= startOfMonth && x.AllocationDate <= date)
            .GroupBy(x => x.TenantId)
            .Select(g => new { TenantId = g.Key, Amount = g.Sum(x => x.InterestApplied + x.FeesApplied) })
            .ToDictionaryAsync(x => x.TenantId, x => x.Amount, ct);

        var revenueYtdByTenant = await _db.LoanPaymentAllocations
            .Where(x => x.AllocationDate >= startOfYear && x.AllocationDate <= date)
            .GroupBy(x => x.TenantId)
            .Select(g => new { TenantId = g.Key, Amount = g.Sum(x => x.InterestApplied + x.FeesApplied) })
            .ToDictionaryAsync(x => x.TenantId, x => x.Amount, ct);

        var paymentsDueByTenant = await _db.Installments
            .Where(x => x.DueDate >= startOfMonth && x.DueDate <= date)
            .GroupBy(x => x.Instrument.TenantId)
            .Select(g => new { TenantId = g.Key, Amount = g.Sum(x => x.TotalAmount.Amount) })
            .ToDictionaryAsync(x => x.TenantId, x => x.Amount, ct);

        var paymentsCollectedByTenant = await _db.Payments
            .Where(x => x.PaymentDate >= startOfMonth && x.PaymentDate <= date && x.Status == PaymentStatus.Completed)
            .GroupBy(x => x.TenantId)
            .Select(g => new { TenantId = g.Key, Amount = g.Sum(x => x.Amount.Amount) })
            .ToDictionaryAsync(x => x.TenantId, x => x.Amount, ct);

        foreach (var agg in aggregates)
        {
            var nplRatio = agg.TotalOutstanding > 0 ? agg.NplOutstanding / agg.TotalOutstanding : 0;

            var metrics = new PortfolioMetricsDaily(agg.TenantId, date);

            metrics.SetTotals(
                agg.TotalLoans,
                agg.TotalOutstanding,
                agg.TotalPrincipal,
                agg.TotalInterest,
                agg.TotalLateFees
            );

            metrics.SetRisk(
                nplRatio,
                agg.Bucket0To30,
                agg.Bucket31To60,
                agg.Bucket61To90,
                agg.Bucket90Plus
            );

            var revMTD = revenueMtdByTenant.GetValueOrDefault(agg.TenantId, 0m);
            var revYTD = revenueYtdByTenant.GetValueOrDefault(agg.TenantId, 0m);
            metrics.SetRevenue(revMTD, revYTD);

            var pDue = paymentsDueByTenant.GetValueOrDefault(agg.TenantId, 0m);
            var pCol = paymentsCollectedByTenant.GetValueOrDefault(agg.TenantId, 0m);
            var ce = pDue > 0 ? pCol / pDue : 0;
            metrics.SetCollections(ce);

            _db.PortfolioMetricsDaily.Add(metrics);
        }

        await _db.SaveChangesAsync(ct);
    }
}

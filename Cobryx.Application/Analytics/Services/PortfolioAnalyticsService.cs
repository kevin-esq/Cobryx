using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Payments.Enums;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Analytics.Services;

public interface IPortfolioAnalyticsService
{
    public Task<PortfolioMetricsDaily> CalculateNightlyMetricsAsync(Guid tenantId, DateTime date, CancellationToken ct = default);
}

public class PortfolioAnalyticsService : IPortfolioAnalyticsService
{
    private readonly ICobryxDbContext _db;

    public PortfolioAnalyticsService(ICobryxDbContext db)
    {
        _db = db;
    }

    public async Task<PortfolioMetricsDaily> CalculateNightlyMetricsAsync(Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        // 1. Get the latest snapshot per active loan using Window SQL Functions for O(n) performance
        var latestSnapshots = await _db.LatestLoanSnapshots
            .FromSqlInterpolated($@"
                WITH latest_snapshots AS (
                    SELECT *,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""LoanId""
                            ORDER BY ""RecordedAt"" DESC
                        ) as rn
                    FROM ""LoanBalanceSnapshots""
                    WHERE ""TenantId"" = {tenantId}
                )
                SELECT *
                FROM latest_snapshots
                WHERE rn = 1
                AND (""PrincipalBalance"" + ""InterestBalance"" + ""LateFeeBalance"") > 0
            ")
            .ToListAsync(ct);

        // 2. Portfolio Totals Aggregation
        var totalLoans = latestSnapshots.Count;
        var totalPrincipal = latestSnapshots.Sum(x => x.PrincipalBalance);
        var totalInterest = latestSnapshots.Sum(x => x.InterestBalance);
        var totalLateFees = latestSnapshots.Sum(x => x.LateFeeBalance);
        var totalOutstanding = totalPrincipal + totalInterest + totalLateFees;

        // 3. Risk / NPL Ratio
        var nplOutstanding = latestSnapshots
            .Where(x => x.DaysPastDue >= 90)
            .Sum(x => x.Outstanding);

        var nplRatio = totalOutstanding > 0 ? nplOutstanding / totalOutstanding : 0;

        // 4. Aging Buckets
        var bucket0to30 = latestSnapshots.Where(x => x.DaysPastDue <= 30).Sum(x => x.Outstanding);
        var bucket31to60 = latestSnapshots.Where(x => x.DaysPastDue >= 31 && x.DaysPastDue <= 60).Sum(x => x.Outstanding);
        var bucket61to90 = latestSnapshots.Where(x => x.DaysPastDue >= 61 && x.DaysPastDue <= 90).Sum(x => x.Outstanding);
        var bucket90plus = latestSnapshots.Where(x => x.DaysPastDue > 90).Sum(x => x.Outstanding);

        // 5. Revenue MTD / YTD
        var startOfMonth = new DateTime(date.Year, date.Month, 1);
        var startOfYear = new DateTime(date.Year, 1, 1);

        var revenueMTD = await _db.LoanPaymentAllocations
            .Where(x => x.TenantId == tenantId && x.AllocationDate >= startOfMonth && x.AllocationDate <= date)
            .SumAsync(x => x.InterestApplied + x.FeesApplied, ct);

        var revenueYTD = await _db.LoanPaymentAllocations
            .Where(x => x.TenantId == tenantId && x.AllocationDate >= startOfYear && x.AllocationDate <= date)
            .SumAsync(x => x.InterestApplied + x.FeesApplied, ct);

        // 6. Collection Efficiency
        var paymentsDue = await _db.Installments
            .Where(x => x.Instrument.TenantId == tenantId && x.DueDate >= startOfMonth && x.DueDate <= date)
            .SumAsync(x => x.TotalAmount.Amount, ct);

        // We assume `_db.Payments` resolves to Credit/Installment payments or we join with allocations
        // The architect meant all payments successfully recorded.
        var paymentsCollected = await _db.Payments
            .Where(x => x.TenantId == tenantId && x.PaymentDate >= startOfMonth && x.PaymentDate <= date && x.Status == PaymentStatus.Completed)
            .SumAsync(x => x.Amount.Amount, ct);

        var collectionEfficiency = paymentsDue > 0 ? paymentsCollected / paymentsDue : 0;

        // 7. Persist
        var metrics = new PortfolioMetricsDaily(tenantId, date);
        metrics.SetTotals(totalLoans, totalOutstanding, totalPrincipal, totalInterest, totalLateFees);
        metrics.SetRisk(nplRatio, bucket0to30, bucket31to60, bucket61to90, bucket90plus);
        metrics.SetRevenue(revenueMTD, revenueYTD);
        metrics.SetCollections(collectionEfficiency);

        _db.PortfolioMetricsDaily.Add(metrics);
        await _db.SaveChangesAsync(ct);

        return metrics;
    }
}

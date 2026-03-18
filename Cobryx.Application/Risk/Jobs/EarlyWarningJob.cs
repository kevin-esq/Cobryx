using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Analytics.Risk;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Risk.Jobs;

public class EarlyWarningJob
{
    private readonly ICobryxDbContext _db;
    private readonly ProbabilityOfDefaultCalculator _pdCalculator;

    public EarlyWarningJob(
        ICobryxDbContext db,
        ProbabilityOfDefaultCalculator pdCalculator)
    {
        _db = db;
        _pdCalculator = pdCalculator;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var sql = @"
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
    WHERE ""RecordedAt"" >= NOW() - INTERVAL '1 day'
)
SELECT ""TenantId"", ""LoanId"", ""PrincipalBalance"", ""InterestBalance"", ""LateFeeBalance"", ""DaysPastDue""
FROM latest_snapshots
WHERE rn = 1;";

        var snapshots = await _db.LatestLoanSnapshots
            .FromSqlRaw(sql)
            .ToListAsync(ct);

        var events = new List<RiskEvent>();
        
        foreach (var s in snapshots)
        {
            // skip already delinquent → collections handles it
            if (s.DaysPastDue > 0) continue;

            var context = new RiskContext
            {
                DaysPastDue = s.DaysPastDue,
                Outstanding = s.Outstanding,
                CreditLimit = s.Outstanding == 0 ? 1 : s.Outstanding, // safe fallback
                Utilization = 1m,
                PaymentDelayDays = 0,
                PreviousPaymentDelayDays = 0,
                PreviousUtilization = 0
            };

            var currentPD = _pdCalculator.Calculate(context);
            
            // Dummy previous PD logic to satisfy deterioration (fetch real previous PD later)
            var previousPD = currentPD * 0.9m; 
            var deltaUtilization = 0m; 
            var deltaPaymentDelay = 0m;

            var deterioration = (currentPD - previousPD) + deltaUtilization + (deltaPaymentDelay / 30m);

            // thresholds configurable per tenant later
            if (currentPD < 0.6m && deterioration < 0.1m) continue;

            // Evitar duplicados recientes
            var exists = await _db.RiskEvents.AnyAsync(x =>
                x.CustomerId == s.TenantId && // Note: mapping Loan -> Customer later, using TenantId per snippet
                x.EventType == RiskEventType.BalanceIncrease &&
                x.OccurredAt >= DateTime.UtcNow.AddHours(-6),
                ct);

            if (exists) continue;

            var riskEvent = new RiskEvent(
                s.TenantId,
                RiskEventType.BalanceIncrease,
                currentPD
            );

            events.Add(riskEvent);
        }

        // Batch Save
        if (events.Count > 0)
        {
            _db.RiskEvents.AddRange(events);
            await _db.SaveChangesAsync(ct);
        }
    }
}

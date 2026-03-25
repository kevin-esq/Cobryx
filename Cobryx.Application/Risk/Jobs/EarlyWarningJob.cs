using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Domain.Analytics.Risk;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Risk.Jobs;

public class EarlyWarningJob
{
    private readonly ICobryxDbContext _db;
    private readonly ProbabilityOfDefaultCalculator _pdCalculator;
    private readonly DecisionService _decisionService;
    private readonly ILogger<EarlyWarningJob> _logger;

    public EarlyWarningJob(
        ICobryxDbContext db,
        ProbabilityOfDefaultCalculator pdCalculator,
        DecisionService decisionService,
        ILogger<EarlyWarningJob> logger)
    {
        _db = db;
        _pdCalculator = pdCalculator;
        _decisionService = decisionService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var sql = @"
WITH latest_snapshots AS (
    SELECT
        s.""TenantId"",
        s.""LoanId"",
        l.""CustomerId"",
        s.""PrincipalBalance"",
        s.""InterestBalance"",
        s.""LateFeeBalance"",
        s.""DaysPastDue"",
        ROW_NUMBER() OVER (
            PARTITION BY s.""LoanId""
            ORDER BY s.""RecordedAt"" DESC
        ) AS rn
    FROM ""LoanBalanceSnapshots"" s
    INNER JOIN ""Loans"" l ON s.""LoanId"" = l.""Id""
    WHERE s.""RecordedAt"" >= NOW() - INTERVAL '1 day'
      AND s.""DaysPastDue"" <= 0
)
SELECT ""TenantId"", ""LoanId"", ""CustomerId"", ""PrincipalBalance"", ""InterestBalance"", ""LateFeeBalance"", ""DaysPastDue""
FROM latest_snapshots
WHERE rn = 1;";

        var snapshots = await _db.LatestLoanSnapshots
            .FromSqlRaw(sql)
            .ToListAsync(ct);

        var events = new List<RiskEvent>();
        var threshold = 0.6m;
        var pdCache = new Dictionary<Guid, decimal>();

        foreach (var s in snapshots)
        {
            if (s.DaysPastDue > 0)
                continue;

            if (!pdCache.TryGetValue(s.LoanId, out var currentPD))
            {
                var context = new RiskContext
                {
                    DaysPastDue = s.DaysPastDue,
                    Outstanding = s.Outstanding,
                    CreditLimit = s.Outstanding == 0 ? 1m : (s.Outstanding * 2m),
                    Utilization = 1m,
                    PaymentDelayDays = 0,
                    PreviousPaymentDelayDays = 0,
                    PreviousUtilization = 0
                };

                currentPD = _pdCalculator.Calculate(context);
                pdCache[s.LoanId] = currentPD;
            }

            var previousPD = currentPD * 0.9m;
            var deltaUtilization = 0m;
            var deltaPaymentDelay = 0m;

            var rawDeterioration = (currentPD - previousPD) + (deltaUtilization * 0.5m) + (deltaPaymentDelay / 30m * 0.5m);
            var deterioration = Math.Clamp(rawDeterioration, 0m, 1m);

            if (currentPD < threshold && deterioration < 0.1m)
                continue;

            var decision = await _decisionService.EvaluateAsync(s.CustomerId, new Cobryx.Domain.Decision.DecisionContext
            {
                Credit = new Cobryx.Domain.Decision.CreditContext
                {
                    ProbabilityOfDefault = currentPD,
                    BehaviorScore = 1m,
                    MonthlyIncomeEstimate = 10000m,
                    Utilization = 0.5m
                },
                Pricing = new Cobryx.Domain.Decision.PricingContext
                {
                    ProbabilityOfDefault = currentPD
                },
                Fraud = new Cobryx.Domain.Decision.FraudContext
                {
                    TransactionsLastHour = 2,
                    AmountVelocity = 500m,
                    GeoAnomaly = false
                }
            });

            _logger.LogInformation("Decision rendered for Anomaly Loan {LoanId}: Limit {Limit}, Rate {Rate}, Fraud {Fraud}, Approved {Approved}",
                s.LoanId, decision.CreditLimit, decision.InterestRate, decision.FraudScore, decision.Approved);

            var exists = await _db.RiskEvents.AnyAsync(x =>
                x.CustomerId == s.CustomerId &&
                x.EventType == RiskEventType.BalanceIncrease &&
                x.OccurredAt >= DateTime.UtcNow.AddHours(-6),
                ct);

            if (exists)
                continue;

            _logger.LogInformation("EarlyWarning triggered for Loan {LoanId} with PD {PD} and Deterioration {Deterioration}", s.LoanId, currentPD, deterioration);

            var riskEvent = new RiskEvent(
                s.CustomerId,
                RiskEventType.BalanceIncrease,
                currentPD
            );

            events.Add(riskEvent);
        }

        if (events.Count > 0)
        {
            await using var tx = await _db.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

            _db.RiskEvents.AddRange(events);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
    }
}

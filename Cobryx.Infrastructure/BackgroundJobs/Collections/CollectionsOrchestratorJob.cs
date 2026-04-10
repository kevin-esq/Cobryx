using System.Text.Json;

using Cobryx.Application.Collections.Assignment;
using Cobryx.Application.Collections.Models;
using Cobryx.Application.Collections.Optimizer;
using Cobryx.Application.Collections.Strategy;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Collections;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Cobryx.Infrastructure.BackgroundJobs.Collections;

public partial class CollectionsOrchestratorJob(
    ICobryxDbContext dbContext,
    ICollectionsStrategyEngine strategyEngine,
    IAssignmentEngine assignmentEngine,
    IConnectionMultiplexer redis,
    IClock clock,
    ILogger<CollectionsOrchestratorJob> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task ProcessCollectionsAsync()
    {
        LogJobStarted(logger);

        var delinquentLoans = await dbContext.LatestLoanSnapshots
            .Where(s => s.DaysPastDue > 0)
            .Join(dbContext.Loans,
                s => s.LoanId,
                l => l.Id,
                (s, l) => new { Snapshot = s, l.TenantId })
            .ToListAsync();

        LogDelinquentLoansFound(logger, delinquentLoans.Count);

        foreach (var data in delinquentLoans)
        {
            try
            {
                var snapshot = data.Snapshot;
                Guid tenantId = data.TenantId;

                CustomerRiskProfile riskProfile = new() { Score = 50 };
                PaymentBehaviorProfile behaviorProfile = new() { MissedPayments = 1, PaymentConsistencyScore = 0.8m };
                DpdTrend trend = new() { CurrentDpd = snapshot.DaysPastDue, PreviousDpd = Math.Max(0, snapshot.DaysPastDue - 1) };

                var db = redis.GetDatabase();
                var weightsJson = await db.StringGetAsync($"portfolio:collections:weights:{tenantId}");
                var weights = new StrategyWeights();
                if (weightsJson.HasValue)
                {
                    var deserialized = JsonSerializer.Deserialize<StrategyWeights>(weightsJson!);
                    if (deserialized != null)
                        weights = deserialized;
                }

                var decision = strategyEngine.Evaluate(
                    snapshot.DaysPastDue,
                    snapshot.Outstanding,
                    riskProfile,
                    behaviorProfile,
                    trend,
                    weights,
                    clock);

                var collectionCase = await dbContext.CollectionCases
                    .FirstOrDefaultAsync(c => c.LoanId == snapshot.LoanId && !c.IsClosed);

                if (collectionCase == null)
                {
                    collectionCase = new CollectionCase(
                        tenantId,
                        snapshot.LoanId,
                        snapshot.DaysPastDue,
                        snapshot.Outstanding);

                    collectionCase.ApplyDecision(decision.Stage, decision.PriorityScore, decision.NextActionAt);
                    dbContext.CollectionCases.Add(collectionCase);
                }
                else
                {
                    collectionCase.SyncState(snapshot.DaysPastDue, snapshot.Outstanding);
                    collectionCase.ApplyDecision(decision.Stage, decision.PriorityScore, decision.NextActionAt);
                }

                if (collectionCase.NextActionAt == null || collectionCase.NextActionAt <= clock.UtcNow)
                {
                    var action = new CollectionAction(
                        collectionCase.Id,
                        decision.Action,
                        $"Automated {decision.Action} triggered by Strategy Engine. Priority: {decision.PriorityScore}");

                    dbContext.CollectionActions.Add(action);

                    collectionCase.ApplyDecision(decision.Stage, decision.PriorityScore, clock.UtcNow.AddDays(1));
                }

                var priorityKey = $"portfolio:collections:priority:{tenantId}";
                var dataKey = $"portfolio:collections:data:{snapshot.LoanId}";

                await db.SortedSetAddAsync(priorityKey, snapshot.LoanId.ToString(), decision.PriorityScore);

                var metadata = JsonSerializer.Serialize(new
                {
                    loanId = snapshot.LoanId,
                    dpd = snapshot.DaysPastDue,
                    outstanding = snapshot.Outstanding,
                    stage = decision.Stage.ToString()
                }, _jsonOptions);

                await db.HashSetAsync(dataKey, "info", metadata);
            }
            catch (Exception ex)
            {
                LogProcessLoanFailed(logger, ex, data.Snapshot.LoanId);
            }
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var affectedTenants = delinquentLoans.Select(x => x.TenantId).Distinct().ToList();
        foreach (Guid tId in affectedTenants)
        {
            await assignmentEngine.AssignCasesAsync(tId);
        }

        LogJobCompleted(logger);
    }
}

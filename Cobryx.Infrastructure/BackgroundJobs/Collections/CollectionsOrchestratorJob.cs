using System.Text.Json;
using Cobryx.Application.Collections.Assignment;
using Cobryx.Application.Collections.Models;
using Cobryx.Application.Collections.Strategy;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Cobryx.Infrastructure.BackgroundJobs.Collections;

public class CollectionsOrchestratorJob
{
    private readonly ICobryxDbContext _dbContext;
    private readonly ICollectionsStrategyEngine _strategyEngine;
    private readonly IAssignmentEngine _assignmentEngine;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CollectionsOrchestratorJob> _logger;

    public CollectionsOrchestratorJob(
        ICobryxDbContext dbContext,
        ICollectionsStrategyEngine strategyEngine,
        IAssignmentEngine assignmentEngine,
        IConnectionMultiplexer redis,
        ILogger<CollectionsOrchestratorJob> logger)
    {
        _dbContext = dbContext;
        _strategyEngine = strategyEngine;
        _assignmentEngine = assignmentEngine;
        _redis = redis;
        _logger = logger;
    }

    public async Task ProcessCollectionsAsync()
    {
        _logger.LogInformation("Starting Collections Orchestrator Job...");

        var delinquentLoans = await _dbContext.LatestLoanSnapshots
            .Where(s => s.DaysPastDue > 0)
            .Join(_dbContext.Loans,
                s => s.LoanId,
                l => l.Id,
                (s, l) => new { Snapshot = s, l.TenantId })
            .ToListAsync();

        _logger.LogInformation($"Found {delinquentLoans.Count} delinquent loans.");

        foreach (var data in delinquentLoans)
        {
            try
            {
                var snapshot = data.Snapshot;
                var tenantId = data.TenantId;

                // Mocks for Phase 10 logic
                var riskProfile = new CustomerRiskProfile { Score = 50 };
                var behaviorProfile = new PaymentBehaviorProfile { MissedPayments = 1, PaymentConsistencyScore = 0.8m };
                var trend = new DpdTrend { CurrentDpd = snapshot.DaysPastDue, PreviousDpd = System.Math.Max(0, snapshot.DaysPastDue - 1) };

                // ML Weights
                var db = _redis.GetDatabase();
                var weightsJson = await db.StringGetAsync($"portfolio:collections:weights:{tenantId}");
                var weights = new Cobryx.Application.Collections.Optimizer.StrategyWeights();
                if (weightsJson.HasValue) 
                {
                    var deserialized = System.Text.Json.JsonSerializer.Deserialize<Cobryx.Application.Collections.Optimizer.StrategyWeights>(weightsJson!);
                    if (deserialized != null) weights = deserialized;
                }

                // 1. STRATEGY EVALUATION
                var decision = _strategyEngine.Evaluate(
                    snapshot.DaysPastDue, 
                    snapshot.Outstanding, 
                    riskProfile, 
                    behaviorProfile, 
                    trend,
                    weights);

                // 2. CASE MANAGEMENT
                var collectionCase = await _dbContext.CollectionCases
                    .FirstOrDefaultAsync(c => c.LoanId == snapshot.LoanId && !c.IsClosed);

                if (collectionCase == null)
                {
                    collectionCase = new CollectionCase(
                        tenantId,
                        snapshot.LoanId,
                        snapshot.DaysPastDue,
                        snapshot.Outstanding);

                    collectionCase.ApplyDecision(decision.Stage, decision.PriorityScore, decision.NextActionAt);
                    _dbContext.CollectionCases.Add(collectionCase);
                }
                else
                {
                    collectionCase.SyncState(snapshot.DaysPastDue, snapshot.Outstanding);
                    collectionCase.ApplyDecision(decision.Stage, decision.PriorityScore, decision.NextActionAt);
                }

                // 3. ACTION LOGGING
                if (collectionCase.NextActionAt == null || collectionCase.NextActionAt <= System.DateTime.UtcNow)
                {
                    var action = new CollectionAction(
                        collectionCase.Id,
                        decision.Action,
                        $"Automated {decision.Action} triggered by Strategy Engine. Priority: {decision.PriorityScore}");

                    _dbContext.CollectionActions.Add(action);

                    // Push next action date 24h into the future
                    collectionCase.ApplyDecision(decision.Stage, decision.PriorityScore, System.DateTime.UtcNow.AddDays(1));
                }

                // 4. REDIS PRIORITY QUEUE (ZSET + HASH)
                var priorityKey = $"portfolio:collections:priority:{tenantId}";
                var dataKey = $"portfolio:collections:data:{snapshot.LoanId}";

                await db.SortedSetAddAsync(priorityKey, snapshot.LoanId.ToString(), decision.PriorityScore);

                var metadata = JsonSerializer.Serialize(new
                {
                    loanId = snapshot.LoanId,
                    dpd = snapshot.DaysPastDue,
                    outstanding = snapshot.Outstanding,
                    stage = decision.Stage.ToString()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await db.HashSetAsync(dataKey, "info", metadata);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Failed to process collection case for Loan {data.Snapshot.LoanId}");
            }
        }

        await _dbContext.SaveChangesAsync(default);

        // 5. AUTO ASSIGNMENT
        var affectedTenants = delinquentLoans.Select(x => x.TenantId).Distinct().ToList();
        foreach (var tId in affectedTenants)
        {
            await _assignmentEngine.AssignCasesAsync(tId);
        }

        _logger.LogInformation("Collections Orchestrator Job completed successfully.");
    }
}

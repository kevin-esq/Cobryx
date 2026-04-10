using Cobryx.Application.Collections.Optimizer;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Collections;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Collections;

public class CollectionOptimizer : ICollectionOptimizer
{
    private readonly ICobryxDbContext _dbContext;
    private readonly ILogger<CollectionOptimizer> _logger;

    public CollectionOptimizer(ICobryxDbContext dbContext, ILogger<CollectionOptimizer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StrategyWeights> CalculateWeightsAsync(Guid tenantId)
    {
        var outcomes = await _dbContext.CollectionOutcomes
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.Id)
            .Take(1000)
            .ToListAsync();

        var weights = new StrategyWeights();

        if (!outcomes.Any())
            return weights;

        var smsSuccess = CalculateSuccessRate(outcomes, CollectionActionType.SmsReminder);
        var emailSuccess = CalculateSuccessRate(outcomes, CollectionActionType.EmailReminder);
        var callSuccess = CalculateSuccessRate(outcomes, CollectionActionType.AgentCall);
        var legalSuccess = CalculateSuccessRate(outcomes, CollectionActionType.LegalNotice);

        weights.SmsWeight = Math.Max(0.5m, smsSuccess * 2.0m);
        weights.EmailWeight = Math.Max(0.5m, emailSuccess * 2.0m);
        weights.CallWeight = Math.Max(0.5m, callSuccess * 2.0m);
        weights.LegalWeight = Math.Max(0.5m, legalSuccess * 2.0m);

        _logger.LogInformation($"Computed new ML Weights for Tenant {tenantId}: SMS={weights.SmsWeight:F2}, Email={weights.EmailWeight:F2}, Call={weights.CallWeight:F2}, Legal={weights.LegalWeight:F2}");

        return weights;
    }

    private decimal CalculateSuccessRate(System.Collections.Generic.List<CollectionOutcome> outcomes, CollectionActionType type)
    {
        var typeOutcomes = outcomes.Where(o => o.ActionType == type).ToList();
        if (!typeOutcomes.Any())
            return 0.5m;
        return (decimal)typeOutcomes.Count(o => o.WasSuccessful) / typeOutcomes.Count;
    }
}

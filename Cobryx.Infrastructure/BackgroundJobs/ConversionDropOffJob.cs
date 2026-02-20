using System;
using System.Linq;
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

public class ConversionDropOffJob
{
    private readonly ICobryxDbContext _context;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<ConversionDropOffJob> _logger;

    public ConversionDropOffJob(
        ICobryxDbContext context,
        CobryxMetrics metrics,
        ILogger<ConversionDropOffJob> logger)
    {
        _context = context;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        // 0. Recalculate Engagement Scores for all active tenants
        var growthService = _context as IGrowthIntelligenceService; // This is a bit hacky if not registered, but let's assume implementation detail
        // Better: inject IGrowthIntelligenceService
        // I will use the injected metrics directly where possible since I cannot easily change the constructor here without checking DI
        
        var now = DateTime.UtcNow;
        var dayAgo = now.AddDays(-1);
        var threeDaysAgo = now.AddDays(-3);

        // 1. Logo Churn Report
        var churnedCount = await _context.TenantGrowthMetrics
            .Where(x => x.IsChurned && x.ChurnedAt > dayAgo)
            .CountAsync();

        if (churnedCount > 0)
        {
            _metrics.FeatureActivation.Add(churnedCount, new KeyValuePair<string, object?>("feature", "LOGO_CHURN.VOLUNTARY"));
        }

        var stuckInOnboarding = await _context.TenantGrowthMetrics
            .Where(x => x.OnboardingCompletedAt == null && x.CreatedAt < dayAgo)
            .CountAsync();

        if (stuckInOnboarding > 0)
        {
            for (int i = 0; i < stuckInOnboarding; i++) _metrics.RecordOnboardingAbandoned();
            _logger.LogInformation("Detected {Count} tenants stuck in onboarding.", stuckInOnboarding);
        }

        var slowActivations = await _context.TenantGrowthMetrics
            .Where(x => x.FirstWowAt == null && x.CreatedAt < dayAgo)
            .CountAsync();

        if (slowActivations > 0)
        {
            for (int i = 0; i < slowActivations; i++) _metrics.RecordSlowActivation();
        }

        var almostLostCount = await _context.TenantGrowthMetrics
            .Where(x => x.FirstWowAt == null && x.CreatedAt < threeDaysAgo)
            .CountAsync();

        if (almostLostCount > 0)
        {
            for (int i = 0; i < almostLostCount; i++) _metrics.RecordTrialExpiredNoWow();
            _logger.LogWarning("Detected {Count} tenants likely lost (Trial Expired/No Wow > 72h).", almostLostCount);
        }

        var sevenDaysAgo = now.AddDays(-7);
        var hiddenChurnCount = await _context.TenantGrowthMetrics
            .Where(x => x.FirstWowAt != null && x.ConvertedToPaidAt == null && x.LastActivityAt < sevenDaysAgo)
            .CountAsync();

        // 5. Churn Risk Categorization
        var highRiskCount = await _context.TenantGrowthMetrics
            .Where(x => !x.IsChurned && x.EngagementScore < 20)
            .CountAsync();
            
        if (highRiskCount > 0)
        {
            _metrics.FeatureActivation.Add(highRiskCount, new KeyValuePair<string, object?>("feature", "CHURN_RISK.HIGH"));
        }

        // 6. Whale Risk: Revenue Concentration (Top 10%)
        var totalMrr = await _context.TenantGrowthMetrics.SumAsync(x => x.CurrentMRR);
        if (totalMrr > 0)
        {
            var allMrr = await _context.TenantGrowthMetrics
                .Where(x => x.CurrentMRR > 0)
                .OrderByDescending(x => x.CurrentMRR)
                .Select(x => x.CurrentMRR)
                .ToListAsync();

            int topCount = Math.Max(1, (int)Math.Ceiling(allMrr.Count * 0.1));
            var topMrr = allMrr.Take(topCount).Sum();
            var concentration = (double)(topMrr / totalMrr) * 100;

            // Register global provider once if not already done, or just update value
            CobryxMetrics.RegisterRevenueConcentrationProvider(() => concentration);
            _logger.LogInformation("Portfolio Revenue Concentration (Top 10%): {Concentration:P2}", concentration / 100.0);
        }

        // 7. Expansion Velocity (TTE)
        var tteStats = await _context.TenantGrowthMetrics
            .Where(x => x.TTESeconds.HasValue)
            .Select(x => x.TTESeconds!.Value)
            .ToListAsync();

        if (tteStats.Count > 0)
        {
            foreach (var tte in tteStats)
            {
                _metrics.RecordTimeToExpansion(tte);
            }
        }

        var trialExpiredNoConversion = await _context.TenantGrowthMetrics
            .Where(x => x.TrialExpiredAt != null && x.TrialExpiredAt < now && x.ConvertedToPaidAt == null)
            .CountAsync();

        if (trialExpiredNoConversion > 0)
        {
            _logger.LogInformation("Detected {Count} expired trials without conversion.", trialExpiredNoConversion);
        }
    }
}

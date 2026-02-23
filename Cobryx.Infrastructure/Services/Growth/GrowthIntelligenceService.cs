using System;
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Cobryx.Infrastructure.Services.Growth;

public class GrowthIntelligenceService : IGrowthIntelligenceService
{
    private readonly ICobryxDbContext _context;
    private readonly CobryxMetrics _metrics;

    public GrowthIntelligenceService(ICobryxDbContext context, CobryxMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    public async Task RecordWowAsync(Guid tenantId, string outcomeCode)
    {
        var metrics = await GetOrCreateMetricsAsync(tenantId);

        if (metrics.RecordWow(outcomeCode, DateTime.UtcNow))
        {
            await _context.SaveChangesAsync(default);

            // Record to Prometheus
            if (metrics.TTWSeconds.HasValue)
            {
                _metrics.RecordTimeToWow(metrics.TTWSeconds.Value, outcomeCode);
            }
        }
    }

    public async Task MarkOnboardingCompletedAsync(Guid tenantId)
    {
        var metrics = await GetOrCreateMetricsAsync(tenantId);
        metrics.MarkOnboardingCompleted(DateTime.UtcNow);
        await _context.SaveChangesAsync(default);
    }

    public void RecordFeatureActivation(Guid tenantId, string featureName)
    {
        // For feature activation heatmap, we use low-cardinality signals
        // We'll emit this to Prometheus directly
        _metrics.RecordFeatureActivation(featureName);
    }

    public async Task RecordTrialStartAsync(Guid tenantId, int trialDays)
    {
        var metrics = await GetOrCreateMetricsAsync(tenantId);
        metrics.RecordTrialStart(DateTime.UtcNow, trialDays);
        await _context.SaveChangesAsync(default);
    }

    public async Task RecordConversionToPaidAsync(Guid tenantId)
    {
        var metrics = await GetOrCreateMetricsAsync(tenantId);
        metrics.MarkConvertedToPaid(DateTime.UtcNow);
        await _context.SaveChangesAsync(default);

        // Signal conversion to Prometheus
        _metrics.SubscriptionUpgrades.Add(1, new KeyValuePair<string, object?>("tier", "paid"));
    }

    public async Task UpdateTenantActivityAsync(Guid tenantId)
    {
        var metrics = await GetOrCreateMetricsAsync(tenantId);
        metrics.UpdateActivity(DateTime.UtcNow);
        await _context.SaveChangesAsync(default);
    }

    public async Task RecordMRRTransitionAsync(Guid tenantId, decimal newMrr, MRRChangeType changeType, string? reason = null)
    {
        var metrics = await GetOrCreateMetricsAsync(tenantId);

        // Record the history snapshot
        var history = new TenantMRRHistory(tenantId, newMrr, changeType, reason);
        _context.Set<TenantMRRHistory>().Add(history);

        // Update the current metrics
        metrics.RecordMRRTransition(newMrr, changeType);

        await _context.SaveChangesAsync(default);

        // Emit signal to Prometheus
        _metrics.SubscriptionUpgrades.Add(1, new KeyValuePair<string, object?>("tier", changeType.ToString()));
    }

    public async Task CalculateRetentionSignalsAsync()
    {
        var now = DateTime.UtcNow;
        var activeTenants = await _context.TenantGrowthMetrics
            .Where(x => !x.IsChurned)
            .ToListAsync();

        foreach (var tenant in activeTenants)
        {
            // Simple Engagement Logic (0-100)
            // Points based on: 
            // - Recency (Last 7 days activity)
            // - Wow Achievement
            // - Paid status
            int score = 0;

            var daysSinceLastActivity = (now - tenant.LastActivityAt).TotalDays;
            if (daysSinceLastActivity < 1) score += 40;
            else if (daysSinceLastActivity < 3) score += 20;
            else if (daysSinceLastActivity < 7) score += 10;

            if (tenant.FirstWowAt != null) score += 30;
            if (tenant.ConvertedToPaidAt != null) score += 30;

            tenant.UpdateEngagement(score);
        }

        await _context.SaveChangesAsync(default);
    }

    private async Task<TenantGrowthMetrics> GetOrCreateMetricsAsync(Guid tenantId)
    {
        var metrics = await _context.TenantGrowthMetrics
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);

        if (metrics == null)
        {
            // If it doesn't exist, we try to find the tenant to get its creation date
            var tenant = await _context.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == tenantId);

            metrics = new TenantGrowthMetrics(tenantId, tenant?.CreatedAt ?? DateTime.UtcNow);
            _context.TenantGrowthMetrics.Add(metrics);
        }

        return metrics;
    }
}

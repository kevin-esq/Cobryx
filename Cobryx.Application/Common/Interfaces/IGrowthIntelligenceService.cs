using System;
using System.Threading.Tasks;

using Cobryx.Domain.Enums;

namespace Cobryx.Application.Common.Interfaces;

public interface IGrowthIntelligenceService
{
    /// <summary>
    /// Records a value-realization event (Wow event). 
    /// Ensures it is only recorded once per tenant in the database.
    /// Emits the TimeToWow metric.
    /// </summary>
    Task RecordWowAsync(Guid tenantId, string outcomeCode);

    /// <summary>
    /// Marks the tenant's onboarding as completed in the growth metrics.
    /// </summary>
    Task MarkOnboardingCompletedAsync(Guid tenantId);

    /// <summary>
    /// Tracks feature activation intensity for heatmap analysis.
    /// </summary>
    void RecordFeatureActivation(Guid tenantId, string featureName);

    /// <summary>
    /// Records an authoritative MRR transition (New, Expansion, Churn, etc).
    /// </summary>
    Task RecordMRRTransitionAsync(Guid tenantId, decimal newMrr, MRRChangeType changeType, string? reason = null);

    /// <summary>
    /// Calculates complex retention signals (Engagement Score, Active Days) for all active tenants.
    /// Usually called by a background job.
    /// </summary>
    Task CalculateRetentionSignalsAsync();

    /// <summary>
    /// Updates the last activity timestamp for churn prediction.
    /// </summary>
    Task UpdateTenantActivityAsync(Guid tenantId);
}

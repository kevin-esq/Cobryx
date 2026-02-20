using System;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities;

/// <summary>
/// Persistent metrics for tenant growth, activation, and conversion intelligence.
/// This enables deep cohort analysis and product-aware revenue optimization.
/// </summary>
public class TenantGrowthMetrics
{
    public Guid TenantId { get; private set; }
    
    /// <summary>
    /// When the tenant was originally created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the onboarding process was fully completed.
    /// </summary>
    public DateTime? OnboardingCompletedAt { get; private set; }

    /// <summary>
    /// When the first "Wow" event (First Loan/Payment) occurred.
    /// Once set, this is immutable to maintain the integrity of the TTW metric.
    /// </summary>
    public DateTime? FirstWowAt { get; private set; }

    /// <summary>
    /// The specific outcome code that triggered the "Wow" moment.
    /// </summary>
    public string? WowOutcomeCode { get; private set; }

    /// <summary>
    /// Time To Wow in seconds (FirstWowAt - CreatedAt).
    /// </summary>
    public double? TTWSeconds { get; private set; }

    /// <summary>
    /// When the trial period started.
    /// </summary>
    public DateTime? TrialStartedAt { get; private set; }

    /// <summary>
    /// When the trial period is set to expire.
    /// </summary>
    public DateTime? TrialExpiredAt { get; private set; }

    /// <summary>
    /// When the tenant converted to a paid subscription for the first time.
    /// </summary>
    public DateTime? ConvertedToPaidAt { get; private set; }

    /// <summary>
    /// When the tenant made their very first payment (Stripe confirmed).
    /// </summary>
    public DateTime? FirstPaidAt { get; private set; }

    /// <summary>
    /// When the tenant first expanded their revenue (Upgraded plan).
    /// </summary>
    public DateTime? FirstExpansionAt { get; private set; }

    /// <summary>
    /// Time To Expansion in seconds (FirstExpansionAt - FirstPaidAt).
    /// </summary>
    public double? TTESeconds { get; private set; }

    /// <summary>
    /// The last activity timestamp for churn prediction.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// Current Monthly Recurring Revenue.
    /// </summary>
    public decimal CurrentMRR { get; private set; }

    /// <summary>
    /// Cumulative revenue that came from plan upgrades.
    /// </summary>
    public decimal NetExpansionRevenue { get; private set; }

    /// <summary>
    /// Cumulative revenue lost due to plan downgrades.
    /// </summary>
    public decimal NetContractionRevenue { get; private set; }
    
    /// <summary>
    /// Total cumulative revenue from this tenant.
    /// </summary>
    public decimal LifetimeRevenue { get; private set; }

    /// <summary>
    /// Whether the tenant is considered churned (Logo Churn).
    /// </summary>
    public bool IsChurned { get; private set; }

    public DateTime? ChurnedAt { get; private set; }
    public ChurnType ChurnType { get; private set; }

    /// <summary>
    /// Engagement score (0-100) calculated based on activity decay.
    /// </summary>
    public int EngagementScore { get; private set; }

    public ChurnRisk ChurnRisk { get; private set; }

    private TenantGrowthMetrics() { } // EF Core

    public TenantGrowthMetrics(Guid tenantId, DateTime createdAt)
    {
        TenantId = tenantId;
        CreatedAt = createdAt;
        LastActivityAt = createdAt;
    }

    public bool RecordWow(string outcomeCode, DateTime occurredAt)
    {
        if (FirstWowAt != null) return false; // First-Wow Guard

        FirstWowAt = occurredAt;
        WowOutcomeCode = outcomeCode;
        TTWSeconds = (occurredAt - CreatedAt).TotalSeconds;
        LastActivityAt = occurredAt;
        return true;
    }

    public void MarkOnboardingCompleted(DateTime occurredAt)
    {
        OnboardingCompletedAt = occurredAt;
        LastActivityAt = occurredAt;
    }

    public void MarkConvertedToPaid(DateTime occurredAt)
    {
        if (ConvertedToPaidAt == null)
        {
            ConvertedToPaidAt = occurredAt;
        }
        LastActivityAt = occurredAt;
    }

    public void RecordTrialStart(DateTime startedAt, int trialDays)
    {
        TrialStartedAt = startedAt;
        TrialExpiredAt = startedAt.AddDays(trialDays);
    }

    public void UpdateActivity(DateTime occurredAt)
    {
        if (occurredAt > LastActivityAt)
        {
            LastActivityAt = occurredAt;
        }
    }

    public void RecordMRRTransition(decimal newMrr, MRRChangeType changeType)
    {
        var delta = newMrr - CurrentMRR;
        var now = DateTime.UtcNow;

        if (newMrr > 0 && FirstPaidAt == null)
        {
            FirstPaidAt = now;
        }

        if (changeType == MRRChangeType.Expansion && delta > 0)
        {
            NetExpansionRevenue += delta;
            
            if (FirstExpansionAt == null && FirstPaidAt != null)
            {
                FirstExpansionAt = now;
                TTESeconds = (FirstExpansionAt.Value - FirstPaidAt.Value).TotalSeconds;
            }
        }
        else if (changeType == MRRChangeType.Contraction && delta < 0)
        {
            NetContractionRevenue += Math.Abs(delta);
        }

        CurrentMRR = newMrr;
        LifetimeRevenue += Math.Max(0, delta); // Simple approximation for lifetime
        
        IsChurned = newMrr == 0 && changeType == MRRChangeType.Churn;
        if (IsChurned)
        {
            ChurnedAt = DateTime.UtcNow;
            ChurnType = ChurnType.Voluntary;
            ChurnRisk = ChurnRisk.Churned;
        }
    }

    public void UpdateEngagement(int score)
    {
        EngagementScore = Math.Clamp(score, 0, 100);
        
        // Map score to categorical risk
        ChurnRisk = EngagementScore switch
        {
            < 20 => ChurnRisk.High,
            < 60 => ChurnRisk.Medium,
            _ => ChurnRisk.Low
        };

        if (IsChurned) ChurnRisk = ChurnRisk.Churned;
    }
}

using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Events.Subscriptions;

namespace Cobryx.Domain.Entities;

public class TenantSubscription : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public CancellationReason? CancellationReason { get; private set; }
    public string? CancellationFeedback { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? GracePeriodEndsAtUtc { get; private set; }

    // Stripe integration
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public DateTime? TrialEndsAtUtc { get; private set; }

    public virtual SubscriptionPlan Plan { get; private set; } = null!;

    private TenantSubscription() { }

    public TenantSubscription(Guid tenantId, Guid planId, DateTime startDate, DateTime? endDate = null)
    {
        TenantId = tenantId;
        PlanId = planId;
        StartDate = startDate;
        EndDate = endDate;
        Status = SubscriptionStatus.Active;
    }

    public void SetStripeCustomerId(string stripeCustomerId)
    {
        StripeCustomerId = stripeCustomerId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Syncs subscription state from Stripe after checkout completion.
    /// Stripe is the source of truth for trial dates and subscription IDs.
    /// </summary>
    public void SyncFromStripe(string stripeSubscriptionId, SubscriptionStatus status, DateTime? trialEnd, Guid planId)
    {
        StripeSubscriptionId = stripeSubscriptionId;
        PlanId = planId;

        if (status == SubscriptionStatus.Trial && trialEnd.HasValue)
        {
            Status = SubscriptionStatus.Trial;
            TrialEndsAtUtc = trialEnd.Value;
            EndDate = trialEnd.Value;
            AddDomainEvent(new SubscriptionTrialStartedEvent(TenantId, PlanId, trialEnd.Value, DateTime.UtcNow));
        }
        else if (status == SubscriptionStatus.Active)
        {
            Status = SubscriptionStatus.Active;
            TrialEndsAtUtc = null;
            EndDate = null;
        }

        UpdateTimestamp();
    }

    /// <summary>
    /// Transitions from Trial/PastDue to Active after successful payment.
    /// Called when Stripe sends invoice.paid.
    /// </summary>
    public void ActivateFromPayment()
    {
        if (Status == SubscriptionStatus.Active) return; // Idempotent

        Status = SubscriptionStatus.Active;
        TrialEndsAtUtc = null;
        EndDate = null;
        CancelledAtUtc = null;
        GracePeriodEndsAtUtc = null;
        AddDomainEvent(new SubscriptionActivatedEvent(TenantId, PlanId, DateTime.UtcNow));
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks subscription as PastDue when Stripe payment fails.
    /// </summary>
    public void HandlePaymentFailed()
    {
        if (Status == SubscriptionStatus.PastDue) return; // Idempotent

        Status = SubscriptionStatus.PastDue;
        AddDomainEvent(new SubscriptionPaymentFailedEvent(TenantId, PlanId, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void UpdatePlan(SubscriptionPlan newPlan)
    {
        Plan = newPlan;
        PlanId = newPlan.Id;
        Status = SubscriptionStatus.Active;
        EndDate = null;
        CancelledAtUtc = null;
        GracePeriodEndsAtUtc = null;
        UpdateTimestamp();
    }

    public void ExecuteCancellation(DateTime gracePeriodEnd, CancellationReason? reason = null, string? feedback = null)
    {
        if (Status == SubscriptionStatus.Cancelled) return; // Idempotent

        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        GracePeriodEndsAtUtc = gracePeriodEnd;
        CancellationReason = reason;
        CancellationFeedback = feedback;
        AddDomainEvent(new SubscriptionCancelledEvent(TenantId, reason, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void Terminate(DateTime endDate, CancellationReason? reason = null, string? feedback = null)
    {
        EndDate = endDate;
        Status = SubscriptionStatus.Terminated;
        CancellationReason = reason;
        CancellationFeedback = feedback;
        UpdateTimestamp();
    }

    public bool IsActive(DateTime now) => Status == SubscriptionStatus.Active || (Status == SubscriptionStatus.Trial && (EndDate == null || EndDate > now));

    public bool IsExpired(DateTime now) => (Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial) && EndDate != null && EndDate <= now;

    public bool IsWithinGracePeriod(DateTime now) => Status == SubscriptionStatus.Cancelled && GracePeriodEndsAtUtc != null && GracePeriodEndsAtUtc > now;

    public bool IsBlocked(DateTime now) => Status == SubscriptionStatus.Terminated || (Status == SubscriptionStatus.Cancelled && (GracePeriodEndsAtUtc == null || GracePeriodEndsAtUtc <= now));
}

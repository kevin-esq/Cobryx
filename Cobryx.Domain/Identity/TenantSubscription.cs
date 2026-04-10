using Cobryx.Domain.Events.Subscriptions;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;

namespace Cobryx.Domain.Identity;

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
    public void SyncFromStripe(string stripeSubscriptionId, SubscriptionStatus status, DateTime? trialEnd, Guid planId, DateTime now)
    {
        StripeSubscriptionId = stripeSubscriptionId;
        PlanId = planId;

        if (status == SubscriptionStatus.Trial && trialEnd.HasValue)
        {
            Status = SubscriptionStatus.Trial;
            TrialEndsAtUtc = trialEnd.Value;
            EndDate = trialEnd.Value;
            AddDomainEvent(new SubscriptionTrialStartedEvent(TenantId, PlanId, trialEnd.Value, now));
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
    /// Transitions the subscription to 'Active' status (Paid).
    /// Typically called when Stripe confirms the first invoice payment or a renewal.
    /// </summary>
    public void ActivateFromPayment(DateTime now)
    {
        if (Status == SubscriptionStatus.Active)
            return;

        Status = SubscriptionStatus.Active;
        TrialEndsAtUtc = null;
        EndDate = null;
        CancelledAtUtc = null;
        GracePeriodEndsAtUtc = null;
        AddDomainEvent(new SubscriptionActivatedEvent(TenantId, PlanId, now));
        UpdateTimestamp();
    }

    /// <summary>
    /// Flags the subscription as 'PastDue' due to a transient payment failure.
    /// Access is usually preserved during this state until Stripe retries succeed or fail finally.
    /// </summary>
    public void HandlePaymentFailed(DateTime now)
    {
        if (Status == SubscriptionStatus.PastDue)
            return;

        Status = SubscriptionStatus.PastDue;
        AddDomainEvent(new SubscriptionPaymentFailedEvent(TenantId, PlanId, now));
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the subscription plan during a tier change.
    /// </summary>
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

    /// <summary>
    /// Records an intentional cancellation by the user.
    /// The subscription remains technically active until the end of the current billing cycle (Grace Period).
    /// </summary>
    public void ExecuteCancellation(DateTime gracePeriodEnd, DateTime now, CancellationReason? reason = null, string? feedback = null)
    {
        if (Status == SubscriptionStatus.Cancelled)
            return;

        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = now;
        GracePeriodEndsAtUtc = gracePeriodEnd;
        CancellationReason = reason;
        CancellationFeedback = feedback;
        AddDomainEvent(new SubscriptionCancelledEvent(TenantId, reason, now));
        UpdateTimestamp();
    }

    /// <summary>
    /// Hard termination of the subscription (Unpaid or Administrative).
    /// Prevents all access to the application.
    /// </summary>
    public void Terminate(DateTime endDate, CancellationReason? reason = null, string? feedback = null)
    {
        EndDate = endDate;
        Status = SubscriptionStatus.Terminated;
        CancellationReason = reason;
        CancellationFeedback = feedback;
        UpdateTimestamp();
    }

    /// <summary> Checks if the subscription is currently valid (Active or Trial) and not expired. </summary>
    public bool IsActive(DateTime now) => (Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial) && (EndDate == null || EndDate > now);

    /// <summary> Checks if an active subscription has reached its expiration date without renewal. </summary>
    public bool IsExpired(DateTime now) => (Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial) && EndDate != null && EndDate <= now;

    /// <summary> Checks if the user is in the 'Cancelled' grace period awaiting final termination. </summary>
    public bool IsWithinGracePeriod(DateTime now) => Status == SubscriptionStatus.Cancelled && GracePeriodEndsAtUtc != null && GracePeriodEndsAtUtc > now;

    /// <summary> Checks if the subscription is completely blocked from access. </summary>
    public bool IsBlocked(DateTime now) => Status == SubscriptionStatus.Terminated || (Status == SubscriptionStatus.Cancelled && (GracePeriodEndsAtUtc == null || GracePeriodEndsAtUtc <= now));
}

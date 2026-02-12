using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;

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

    public void UpdatePlan(SubscriptionPlan newPlan)
    {
        Plan = newPlan;
        PlanId = newPlan.Id;
        Status = SubscriptionStatus.Active; // Reset status on upgrade/downgrade
        EndDate = null; // Reset expiration
        CancelledAtUtc = null;
        GracePeriodEndsAtUtc = null;
        UpdateTimestamp();
    }

    public void ExecuteCancellation(DateTime gracePeriodEnd, CancellationReason? reason = null, string? feedback = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        GracePeriodEndsAtUtc = gracePeriodEnd;
        CancellationReason = reason;
        CancellationFeedback = feedback;
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

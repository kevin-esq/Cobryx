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

    public void Terminate(DateTime endDate, CancellationReason? reason = null, string? feedback = null)
    {
        EndDate = endDate;
        Status = SubscriptionStatus.Terminated;
        CancellationReason = reason;
        CancellationFeedback = feedback;
        UpdateTimestamp();
    }
}

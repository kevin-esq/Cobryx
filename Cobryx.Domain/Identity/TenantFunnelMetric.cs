using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity.Metrics;

public class TenantFunnelMetric : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string MilestoneCode { get; private set; } = string.Empty;
    public DateTime ReachedAtUtc { get; private set; }
    public double? SecondsSincePrevious { get; private set; }

    private TenantFunnelMetric() { }

    public TenantFunnelMetric(Guid tenantId, string milestoneCode, DateTime reachedAtUtc, double? secondsSincePrevious = null)
    {
        TenantId = tenantId;
        MilestoneCode = milestoneCode;
        ReachedAtUtc = reachedAtUtc;
        SecondsSincePrevious = secondsSincePrevious;
    }
}

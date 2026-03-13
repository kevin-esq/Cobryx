using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;


namespace Cobryx.Domain.Identity;

public class UsageRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public MetricType MetricType { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private UsageRecord() { }

    public UsageRecord(Guid tenantId, MetricType metricType, decimal quantity)
    {
        TenantId = tenantId;
        MetricType = metricType;
        Quantity = quantity;
        RecordedAt = DateTime.UtcNow;
    }
}

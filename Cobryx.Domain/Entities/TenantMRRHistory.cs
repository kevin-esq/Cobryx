using System;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities;

/// <summary>
/// Records a snapshot or transition in a tenant's MRR.
/// This enables historical revenue analysis and growth curves.
/// </summary>
public class TenantMRRHistory
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal MRR { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public MRRChangeType ChangeType { get; private set; }
    public string? Reason { get; private set; }

    private TenantMRRHistory() { } // EF Core

    public TenantMRRHistory(Guid tenantId, decimal mrr, MRRChangeType changeType, string? reason = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        MRR = mrr;
        RecordedAt = DateTime.UtcNow;
        ChangeType = changeType;
        Reason = reason;
    }
}

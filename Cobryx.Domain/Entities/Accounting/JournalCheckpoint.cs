using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Accounting;

public class JournalCheckpoint : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LastProcessedEntryId { get; private set; }
    public string LastFingerprint { get; private set; } = string.Empty;
    public int EntryCount { get; private set; }
    public string HashVersion { get; private set; } = "v1";

    private JournalCheckpoint() { } // EF Core

    public JournalCheckpoint(Guid tenantId, Guid lastProcessedEntryId, string lastFingerprint, int entryCount)
    {
        TenantId = tenantId;
        LastProcessedEntryId = lastProcessedEntryId;
        LastFingerprint = lastFingerprint;
        EntryCount = entryCount;
    }

    public void UpdateCheckpoint(Guid lastEntryId, string fingerprint, int count)
    {
        LastProcessedEntryId = lastEntryId;
        LastFingerprint = fingerprint;
        EntryCount = count;
        UpdateTimestamp();
    }
}

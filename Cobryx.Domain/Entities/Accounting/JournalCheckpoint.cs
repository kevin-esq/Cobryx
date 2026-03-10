using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Accounting;

public class JournalCheckpoint : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LastProcessedEntryId { get; private set; }
    public long LastProcessedSequenceId { get; private set; }
    public DateTime LastEntryCreatedAt { get; private set; }
    public string LastFingerprint { get; private set; } = string.Empty;
    public int EntryCount { get; private set; }
    public string HashVersion { get; private set; } = "v1";

    private JournalCheckpoint() { } // EF Core

    public JournalCheckpoint(Guid tenantId, Guid lastProcessedEntryId, long lastProcessedSequenceId, DateTime lastEntryCreatedAt, string lastFingerprint, int entryCount)
    {
        TenantId = tenantId;
        LastProcessedEntryId = lastProcessedEntryId;
        LastProcessedSequenceId = lastProcessedSequenceId;
        LastEntryCreatedAt = lastEntryCreatedAt;
        LastFingerprint = lastFingerprint;
        EntryCount = entryCount;
    }

    public void UpdateCheckpoint(Guid lastEntryId, long lastSequenceId, DateTime lastEntryCreatedAt, string fingerprint, int count)
    {
        LastProcessedEntryId = lastEntryId;
        LastProcessedSequenceId = lastSequenceId;
        LastEntryCreatedAt = lastEntryCreatedAt;
        LastFingerprint = fingerprint;
        EntryCount = count;
        UpdateTimestamp();
    }
}

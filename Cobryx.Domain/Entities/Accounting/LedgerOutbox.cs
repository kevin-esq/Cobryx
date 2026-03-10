using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// Represents a record in the Transactional Outbox for Ledger events.
/// Ensures at-least-once delivery to external systems (Kafka/EventBus).
/// </summary>
public class LedgerOutbox : BaseEntity
{
    public long JournalSequenceId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty; // JSON Serialized LedgerCdcEvent
    public DateTime? PublishedAt { get; private set; }

    private LedgerOutbox() { } // EF Core

    public LedgerOutbox(long journalSequenceId, string eventType, string payload)
    {
        JournalSequenceId = journalSequenceId;
        EventType = eventType;
        Payload = payload;
        PublishedAt = null;
    }

    public void MarkAsPublished()
    {
        PublishedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}

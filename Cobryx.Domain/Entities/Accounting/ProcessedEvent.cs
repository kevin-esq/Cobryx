using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// Tracks events processed by specific consumers to ensure idempotency (exactly-once processing).
/// </summary>
public class ProcessedEvent : BaseEntity
{
    /// <summary>
    /// The unique ID of the event being processed (e.g. from the Outbox or Bus).
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Unique identifier for the consumer (e.g. "ShadowReplayEngine", "CollectionsEngine").
    /// </summary>
    public string ConsumerName { get; private set; } = string.Empty;

    public DateTime ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(Guid eventId, string consumerName)
    {
        EventId = eventId;
        ConsumerName = consumerName;
        ProcessedAt = DateTime.UtcNow;
    }
}

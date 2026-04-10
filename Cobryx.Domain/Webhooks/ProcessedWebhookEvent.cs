using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Webhooks;

/// <summary>
/// Tracks processed webhook events at the domain level for exactly-once semantics.
/// This is separate from WebhookEvent (ingestion-level) to provide defense-in-depth.
/// Critical for preventing double-processing when:
/// - Webhook retries arrive
/// - API request and webhook race
/// - Outbox consumer retries
/// </summary>
public class ProcessedWebhookEvent : BaseEntity
{
    public string Provider { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string? PaymentIntentId { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedWebhookEvent() { }

    public ProcessedWebhookEvent(
        string provider,
        string eventId,
        string eventType,
        string? paymentIntentId = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        DateTime? now = null)
    {
        Provider = provider;
        EventId = eventId;
        EventType = eventType;
        PaymentIntentId = paymentIntentId;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        ProcessedAt = now ?? DateTime.UtcNow;
    }
}

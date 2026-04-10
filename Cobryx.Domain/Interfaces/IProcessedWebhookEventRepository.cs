using Cobryx.Domain.Webhooks;

namespace Cobryx.Domain.Interfaces;

/// <summary>
/// Repository for domain-level webhook idempotency.
/// Provides exactly-once processing guarantees for webhook handlers.
/// </summary>
public interface IProcessedWebhookEventRepository
{
    /// <summary>
    /// Check if an event has already been processed.
    /// </summary>
    public Task<bool> ExistsAsync(string provider, string eventId, CancellationToken ct = default);

    /// <summary>
    /// Check if a payment intent has already been processed (for API vs webhook race).
    /// </summary>
    public Task<bool> ExistsByPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    /// <summary>
    /// Mark an event as processed. Should be called AFTER successful processing.
    /// </summary>
    public Task AddAsync(ProcessedWebhookEvent processedEvent, CancellationToken ct = default);

    /// <summary>
    /// Atomic check-and-insert. Returns true if this call inserted the record (first processor wins).
    /// </summary>
    public Task<bool> TryMarkAsProcessedAsync(
        string provider,
        string eventId,
        string eventType,
        string? paymentIntentId = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);
}

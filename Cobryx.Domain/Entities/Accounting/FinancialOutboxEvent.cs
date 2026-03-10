using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Accounting.Enums;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// A transactional record of a financial event to be published to the Event Bus.
/// This implements the Transactional Outbox pattern to ensure atomicity between 
/// ledger updates and event emission.
/// </summary>
public class FinancialOutboxEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public FinancialEventType Type { get; private set; }

    /// <summary>
    /// The primary entity involved (e.g., LoanId, PaymentId).
    /// </summary>
    public Guid EntityId { get; private set; }

    /// <summary>
    /// Global deterministic order matching the ledger sequence.
    /// Used for exactly-once processing and replays.
    /// </summary>
    public long LedgerSequenceId { get; private set; }

    /// <summary>
    /// Explicit key for routing in the event bus (typically TenantId).
    /// </summary>
    public string PartitionKey { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Compact JSON payload (< 16KB) containing semantic details.
    /// </summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>
    /// Flag indicating if the event has been successfully published to the bus.
    /// </summary>
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    /// <summary>
    /// Number of failed publication attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    private FinancialOutboxEvent() { }

    public FinancialOutboxEvent(
        Guid tenantId,
        FinancialEventType type,
        Guid entityId,
        long ledgerSequenceId,
        string partitionKey,
        DateTime occurredAt,
        string payloadJson)
    {
        TenantId = tenantId;
        Type = type;
        EntityId = entityId;
        LedgerSequenceId = ledgerSequenceId;
        PartitionKey = partitionKey;
        OccurredAt = occurredAt;
        PayloadJson = payloadJson;
        IsPublished = false;

        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (entityId == Guid.Empty) throw new ArgumentException("EntityId is required", nameof(entityId));
        if (string.IsNullOrWhiteSpace(partitionKey)) throw new ArgumentException("PartitionKey is required", nameof(partitionKey));
        if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException("Payload is required", nameof(payloadJson));
        if (payloadJson.Length > 16384) throw new ArgumentException("Payload exceeds 16KB limit", nameof(payloadJson));
    }

    public void MarkAsPublished(DateTime publishedAt)
    {
        IsPublished = true;
        PublishedAt = publishedAt;
        UpdateTimestamp();
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdateTimestamp();
    }
}

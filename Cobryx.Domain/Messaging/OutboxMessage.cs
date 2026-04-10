using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Messaging;

/// <summary>
/// A unified transactional outbox message for ensuring at-least-once delivery.
/// Handles both standard domain events and strictly ordered financial ledger events.
/// </summary>
public class OutboxMessage : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredOnUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    public bool IsProcessed { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    public Guid? EntityId { get; private set; }
    public long? LedgerSequenceId { get; private set; }
    public string? PartitionKey { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid tenantId, string type, string payload, string? correlationId = null, DateTime? now = null)
    {
        TenantId = tenantId;
        Type = type;
        Payload = payload;
        OccurredOnUtc = now ?? DateTime.UtcNow;
        CorrelationId = correlationId;
        IsProcessed = false;
    }

    public OutboxMessage(
        Guid tenantId,
        string type,
        string payload,
        Guid entityId,
        long ledgerSequenceId,
        string partitionKey,
        string? correlationId = null,
        DateTime? now = null)
    {
        TenantId = tenantId;
        Type = type;
        Payload = payload;
        EntityId = entityId;
        LedgerSequenceId = ledgerSequenceId;
        PartitionKey = partitionKey;
        OccurredOnUtc = now ?? DateTime.UtcNow;
        CorrelationId = correlationId;
        IsProcessed = false;

        if (payload.Length > 16384)
            throw new ArgumentException("Payload exceeds 16KB limit", nameof(payload));
    }

    public void MarkAsProcessed(DateTime processedAt)
    {
        IsProcessed = true;
        ProcessedOnUtc = processedAt;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        IncrementRetry();
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdateTimestamp();
    }
}

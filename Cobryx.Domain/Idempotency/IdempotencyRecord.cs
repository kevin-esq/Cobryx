using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Idempotency;

/// <summary>
/// Persistent idempotency record for ensuring exactly-once execution.
/// Survives Redis restarts and provides audit trail.
/// </summary>
public class IdempotencyRecord : BaseEntity, ITenantEntity
{
    /// <summary>
    /// Processing timeout - if a request is stuck in Processing longer than this,
    /// it's considered abandoned (crash recovery scenario).
    /// Conservative 2 minutes to handle slow external APIs (Stripe, ML inference).
    /// </summary>
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(2);

    public Guid TenantId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string? ResponseBody { get; private set; }
    public int StatusCode { get; private set; }
    public string? ContentType { get; private set; }
    public string? LocationHeader { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? ExternalGatewayId { get; private set; }
    public IdempotencyStatus Status { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }

    private IdempotencyRecord() { }

    public IdempotencyRecord(
        Guid tenantId,
        string idempotencyKey,
        string requestHash,
        TimeSpan ttl,
        DateTime now)
    {
        TenantId = tenantId;
        IdempotencyKey = idempotencyKey;
        RequestHash = requestHash;
        ExpiresAt = now.Add(ttl);
        Status = IdempotencyStatus.Processing;
        ProcessingStartedAt = now;
    }

    public void MarkAsCompleted(int statusCode, string? responseBody, string? contentType, string? locationHeader = null)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        ContentType = contentType;
        LocationHeader = locationHeader;
        Status = IdempotencyStatus.Completed;
        ProcessingStartedAt = null;
        UpdateTimestamp();
    }

    public void MarkAsFailed()
    {
        Status = IdempotencyStatus.Failed;
        ProcessingStartedAt = null;
        UpdateTimestamp();
    }

    public void SetExternalGatewayId(string gatewayId)
    {
        ExternalGatewayId = gatewayId;
        UpdateTimestamp();
    }

    public bool IsExpired(DateTime now) => now > ExpiresAt;

    public bool RequestMatches(string hash) => RequestHash == hash;

    /// <summary>
    /// Checks if a Processing request is stuck (crash recovery).
    /// If stuck longer than timeout, treat as abandoned and allow retry.
    /// </summary>
    public bool IsProcessingStuck(DateTime now)
    {
        if (Status != IdempotencyStatus.Processing)
            return false;

        if (!ProcessingStartedAt.HasValue)
            return true; // No timestamp = legacy record, treat as stuck

        return now - ProcessingStartedAt.Value > ProcessingTimeout;
    }

    /// <summary>
    /// Reset for retry after stuck processing detection.
    /// </summary>
    public void ResetForRetry(DateTime now)
    {
        Status = IdempotencyStatus.Processing;
        ProcessingStartedAt = now;
        UpdateTimestamp(now);
    }
}

public enum IdempotencyStatus
{
    Processing = 0,
    Completed = 1,
    Failed = 2
}

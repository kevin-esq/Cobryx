using Cobryx.Domain.Idempotency;

namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Persistent idempotency store with Redis fast-path and DB fallback.
/// Ensures exactly-once execution even across Redis restarts.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Try to acquire an idempotency lock. Returns existing record if key already exists.
    /// </summary>
    public Task<(bool acquired, IdempotencyRecord? existing)> TryAcquireAsync(
        Guid tenantId,
        string idempotencyKey,
        string requestHash,
        TimeSpan ttl,
        CancellationToken ct = default);

    /// <summary>
    /// Mark the idempotency record as completed with response data.
    /// </summary>
    public Task CompleteAsync(
        Guid tenantId,
        string idempotencyKey,
        int statusCode,
        string? responseBody,
        string? contentType,
        string? locationHeader = null,
        CancellationToken ct = default);

    /// <summary>
    /// Mark the idempotency record as failed (allows retry).
    /// </summary>
    public Task FailAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Associate an external gateway ID with the idempotency key.
    /// Critical for payment double-spend protection.
    /// </summary>
    public Task SetExternalGatewayIdAsync(
        Guid tenantId,
        string idempotencyKey,
        string gatewayId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if an external gateway ID has already been processed.
    /// </summary>
    public Task<bool> ExistsForGatewayIdAsync(
        string gatewayId,
        CancellationToken ct = default);

    /// <summary>
    /// Complete idempotency record within an existing transaction.
    /// CRITICAL: This ensures exactly-once semantics by committing
    /// the business operation and idempotency state atomically.
    /// </summary>
    public Task CompleteWithinTransactionAsync(
        Guid tenantId,
        string idempotencyKey,
        int statusCode,
        string? responseBody,
        string? contentType,
        string? locationHeader = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get the current idempotency context for the request.
    /// Used by handlers to complete idempotency within their transaction.
    /// </summary>
    public IdempotencyContext? GetCurrentContext();

    /// <summary>
    /// Set the current idempotency context for the request.
    /// Called by the filter before executing the handler.
    /// </summary>
    public void SetCurrentContext(Guid tenantId, string idempotencyKey);
}

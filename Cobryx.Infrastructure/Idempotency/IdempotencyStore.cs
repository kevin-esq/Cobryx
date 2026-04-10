using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Idempotency;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Idempotency;

/// <summary>
/// Persistent idempotency store with Redis fast-path and DB fallback.
/// Ensures exactly-once execution even across Redis restarts.
/// </summary>
public partial class IdempotencyStore(
    CobryxDbContext context,
    ICacheService cache,
    IClock clock,
    ILogger<IdempotencyStore> logger) : IIdempotencyStore
{
    private const string CachePrefix = "idem:";

    public async Task<(bool acquired, IdempotencyRecord? existing)> TryAcquireAsync(
        Guid tenantId,
        string idempotencyKey,
        string requestHash,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(tenantId, idempotencyKey);

        // Fast path: check Redis
        var cached = await cache.GetAsync<IdempotencyRecord>(cacheKey, ct);
        if (cached != null)
        {
            if (cached.Status == IdempotencyStatus.Completed && !cached.IsExpired(clock.UtcNow))
            {
                LogIdempotencyHit(logger, idempotencyKey, "cache");
                return (false, cached);
            }

            // CRASH RECOVERY: Check if processing is stuck (>30s)
            if (cached.Status == IdempotencyStatus.Processing && !cached.IsProcessingStuck(clock.UtcNow))
            {
                LogIdempotencyConflict(logger, idempotencyKey);
                return (false, cached);
            }
        }

        // Slow path: check DB (survives Redis restart)
        var existing = await context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

        if (existing != null)
        {
            if (existing.Status == IdempotencyStatus.Completed && !existing.IsExpired(clock.UtcNow))
            {
                // Repopulate cache
                await cache.SetAsync(cacheKey, existing, ttl, ct);
                LogIdempotencyHit(logger, idempotencyKey, "db");
                return (false, existing);
            }

            if (existing.Status == IdempotencyStatus.Processing)
            {
                // CRASH RECOVERY: If stuck >2min, try atomic reset
                if (existing.IsProcessingStuck(clock.UtcNow))
                {
                    // ATOMIC UPDATE: Only one node can win this race
                    var resetResult = await TryAtomicResetForRetryAsync(
                        tenantId, idempotencyKey, existing.ProcessingStartedAt, ct);

                    if (resetResult)
                    {
                        LogIdempotencyStuckRecovery(logger, idempotencyKey);
                        await cache.RemoveAsync(cacheKey, ct);
                        return (true, null);
                    }

                    // Another node won the reset race - refetch and check status
                    var refreshed = await context.IdempotencyRecords
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

                    if (refreshed?.Status == IdempotencyStatus.Processing)
                    {
                        LogIdempotencyConflict(logger, idempotencyKey);
                        return (false, refreshed);
                    }

                    // Status changed (completed/failed) - return it
                    return (false, refreshed);
                }

                // Check if request hash matches (same request retrying)
                if (!existing.RequestMatches(requestHash))
                {
                    LogIdempotencyHashMismatch(logger, idempotencyKey);
                    return (false, existing);
                }

                // Active processing - return conflict
                LogIdempotencyConflict(logger, idempotencyKey);
                return (false, existing);
            }

            // Failed or expired - allow retry by removing old record
            if (existing.Status == IdempotencyStatus.Failed || existing.IsExpired(clock.UtcNow))
            {
                context.IdempotencyRecords.Remove(existing);
                await context.SaveChangesAsync(ct);
            }
        }

        // Create new record with atomic insert
        var record = new IdempotencyRecord(tenantId, idempotencyKey, requestHash, ttl, clock.UtcNow);
        context.IdempotencyRecords.Add(record);

        try
        {
            await context.SaveChangesAsync(ct);
            await cache.SetAsync(cacheKey, record, ttl, ct);
            LogIdempotencyAcquired(logger, idempotencyKey);
            return (true, null);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // RACE CONDITION: Concurrent insert - another request won the race
            // Detach the failed entity to avoid tracking issues
            context.Entry(record).State = EntityState.Detached;

            LogIdempotencyConcurrentInsert(logger, idempotencyKey);

            // Fetch the winner and return conflict
            var winner = await context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

            return (false, winner);
        }
    }

    /// <summary>
    /// Atomic reset for stuck processing records.
    /// Uses optimistic concurrency to ensure only one node wins the race.
    /// </summary>
    private async Task<bool> TryAtomicResetForRetryAsync(
        Guid tenantId,
        string idempotencyKey,
        DateTime? originalProcessingStartedAt,
        CancellationToken ct)
    {
        // Raw SQL for atomic update with WHERE clause
        // Only updates if Status=Processing AND ProcessingStartedAt matches (no one else reset it)
        var now = DateTime.UtcNow;
        var rowsAffected = await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE idempotency_records
            SET status = 0,
                processing_started_at = {0},
                updated_at = {0}
            WHERE tenant_id = {1}
              AND idempotency_key = {2}
              AND status = 0
              AND (processing_started_at = {3} OR processing_started_at IS NULL)
            """,
            [now, tenantId, idempotencyKey, originalProcessingStartedAt ?? (object)DBNull.Value],
            ct);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Detects unique constraint violations across different DB providers.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL: 23505 = unique_violation
        // SQL Server: 2601, 2627 = unique constraint
        // SQLite: 19 = SQLITE_CONSTRAINT
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505") ||
               message.Contains("2601") ||
               message.Contains("2627") ||
               message.Contains("UNIQUE constraint") ||
               message.Contains("duplicate key") ||
               message.Contains("ix_idempotency_records_tenant_key");
    }

    public async Task CompleteAsync(
        Guid tenantId,
        string idempotencyKey,
        int statusCode,
        string? responseBody,
        string? contentType,
        string? locationHeader = null,
        CancellationToken ct = default)
    {
        var record = await context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

        if (record == null)
            return;

        record.MarkAsCompleted(statusCode, responseBody, contentType, locationHeader);
        await context.SaveChangesAsync(ct);

        var cacheKey = BuildCacheKey(tenantId, idempotencyKey);
        var ttl = record.ExpiresAt - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
        {
            await cache.SetAsync(cacheKey, record, ttl, ct);
        }

        LogIdempotencyCompleted(logger, idempotencyKey, statusCode);
    }

    public async Task FailAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        var record = await context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

        if (record == null)
            return;

        record.MarkAsFailed();
        await context.SaveChangesAsync(ct);

        var cacheKey = BuildCacheKey(tenantId, idempotencyKey);
        await cache.RemoveAsync(cacheKey, ct);

        LogIdempotencyFailed(logger, idempotencyKey);
    }

    public async Task SetExternalGatewayIdAsync(
        Guid tenantId,
        string idempotencyKey,
        string gatewayId,
        CancellationToken ct = default)
    {
        var record = await context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

        if (record == null)
            return;

        record.SetExternalGatewayId(gatewayId);
        await context.SaveChangesAsync(ct);

        LogIdempotencyGatewayIdSet(logger, idempotencyKey, gatewayId);
    }

    public async Task<bool> ExistsForGatewayIdAsync(string gatewayId, CancellationToken ct = default)
    {
        return await context.IdempotencyRecords
            .AnyAsync(r => r.ExternalGatewayId == gatewayId && r.Status == IdempotencyStatus.Completed, ct);
    }

    // ============================================
    // TRANSACTION-SCOPED IDEMPOTENCY (EXACTLY-ONCE)
    // ============================================

    private static readonly AsyncLocal<IdempotencyContext?> _currentContext = new();

    /// <summary>
    /// Complete idempotency record within the SAME transaction as the business operation.
    /// This is CRITICAL for exactly-once semantics:
    /// - If business op commits but this fails → retry would re-execute (BAD)
    /// - By being in same transaction → both commit or both rollback (GOOD)
    /// </summary>
    public async Task CompleteWithinTransactionAsync(
        Guid tenantId,
        string idempotencyKey,
        int statusCode,
        string? responseBody,
        string? contentType,
        string? locationHeader = null,
        CancellationToken ct = default)
    {
        // NOTE: This method does NOT call SaveChangesAsync
        // The caller's transaction will commit both the business op AND this update
        var record = await context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, ct);

        if (record == null)
        {
            LogIdempotencyNotFoundForCompletion(logger, idempotencyKey);
            return;
        }

        record.MarkAsCompleted(statusCode, responseBody, contentType, locationHeader);

        // Mark context as completed (filter will skip external CompleteAsync)
        var ctx = _currentContext.Value;
        ctx?.MarkCompleted();

        LogIdempotencyCompletedInTransaction(logger, idempotencyKey, statusCode);

        // Cache update happens after transaction commits (in filter)
    }

    public IdempotencyContext? GetCurrentContext() => _currentContext.Value;

    public void SetCurrentContext(Guid tenantId, string idempotencyKey)
    {
        _currentContext.Value = new IdempotencyContext(tenantId, idempotencyKey);
    }

    public void ClearCurrentContext()
    {
        _currentContext.Value = null;
    }

    private static string BuildCacheKey(Guid tenantId, string key) => $"{CachePrefix}{tenantId}:{key}";

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency hit for key {Key} from {Source}")]
    private static partial void LogIdempotencyHit(ILogger logger, string key, string source);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency conflict for key {Key} - request in progress")]
    private static partial void LogIdempotencyConflict(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency hash mismatch for key {Key} - different request body")]
    private static partial void LogIdempotencyHashMismatch(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency key {Key} was stuck in processing - recovered for retry")]
    private static partial void LogIdempotencyStuckRecovery(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency lock acquired for key {Key}")]
    private static partial void LogIdempotencyAcquired(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency concurrent insert for key {Key} - another request won")]
    private static partial void LogIdempotencyConcurrentInsert(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency completed for key {Key} with status {StatusCode}")]
    private static partial void LogIdempotencyCompleted(ILogger logger, string key, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency failed for key {Key}")]
    private static partial void LogIdempotencyFailed(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency gateway ID set for key {Key}: {GatewayId}")]
    private static partial void LogIdempotencyGatewayIdSet(ILogger logger, string key, string gatewayId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency record not found for completion: {Key}")]
    private static partial void LogIdempotencyNotFoundForCompletion(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency completed IN TRANSACTION for key {Key} with status {StatusCode}")]
    private static partial void LogIdempotencyCompletedInTransaction(ILogger logger, string key, int statusCode);
}

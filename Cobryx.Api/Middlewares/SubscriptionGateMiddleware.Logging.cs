namespace Cobryx.Api.Middlewares;

public partial class SubscriptionGateMiddleware
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Subscription gate blocked mutation for tenant {tenantId}")]
    public static partial void LogMutationBlocked(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Subscription gate: unable to verify subscription for tenant {tenantId}. Failing closed.")]
    public static partial void LogVerificationFailed(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stale cache entry for tenant {tenantId}, falling through to DB")]
    public static partial void LogStaleCacheEntry(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Redis unavailable for subscription gate, falling back to DB")]
    public static partial void LogRedisUnavailable(ILogger logger, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "DB unavailable for subscription gate. Cannot verify tenant {tenantId}")]
    public static partial void LogDbUnavailable(ILogger logger, Exception ex, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to cache subscription status")]
    public static partial void LogCacheUpdateFailed(ILogger logger, Exception ex);
}

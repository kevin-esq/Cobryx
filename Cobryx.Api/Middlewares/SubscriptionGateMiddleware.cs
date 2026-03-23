using System.Text.Json;

using Cobryx.Api.Common;
using Cobryx.Api.Infrastructure;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

namespace Cobryx.Api.Middlewares;

/// <summary>
/// Validates the tenant's subscription status on mutation requests.
/// Runs after TenantMiddleware. Blocks write operations for expired or terminated subscriptions.
/// Read-only (GET/HEAD/OPTIONS) requests pass through unless decorated with [RequiresActiveSubscription].
/// Endpoints decorated with [AllowExpiredSubscription] bypass this gate.
/// </summary>
public class SubscriptionGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionGateMiddleware> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
    private const string CacheKeyPrefix = "subscription_access:";

    public SubscriptionGateMiddleware(RequestDelegate next, ILogger<SubscriptionGateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantSubscriptionRepository subscriptionRepository,
        ICacheService cacheService,
        CobryxMetrics metrics,
        IClock clock)
    {
        var isReadOnly = HttpMethods.IsGet(context.Request.Method) ||
                         HttpMethods.IsHead(context.Request.Method) ||
                         HttpMethods.IsOptions(context.Request.Method);

        if (isReadOnly)
        {
            var ep = context.GetEndpoint();
            if (ep?.Metadata.GetMetadata<RequiresActiveSubscriptionAttribute>() == null)
            {
                await _next(context);
                return;
            }
        }

        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (
            path.StartsWith(ApiEndpoints.Health) ||
            path.StartsWith(ApiEndpoints.Auth) ||
            path.StartsWith(ApiEndpoints.Webhooks) ||
            path.StartsWith(ApiEndpoints.Swagger) ||
            path.StartsWith(ApiEndpoints.Hangfire) ||
            path.StartsWith(ApiEndpoints.Metrics) ||
            path.StartsWith(ApiEndpoints.Ping) ||
            path == ApiEndpoints.Root))
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowExpiredSubscriptionAttribute>() != null)
        {
            await _next(context);
            return;
        }

        if (!context.Items.TryGetValue("Cache_TenantId", out var tenantIdObj) ||
            tenantIdObj is not Guid tenantId ||
            tenantId == Guid.Empty)
        {
            await _next(context);
            return;
        }

        var now = clock.UtcNow;
        var accessInfo = await GetSubscriptionAccessInfo(tenantId, now, subscriptionRepository, cacheService, metrics);
        var isBlocked = accessInfo?.IsBlocked;

        if (accessInfo != null)
        {
            context.Items["Cache_TenantTier"] = accessInfo.Tier;
        }

        if (isBlocked == true)
        {
            metrics.SubscriptionGateBlocked.Add(1,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            _logger.LogWarning("Subscription gate blocked mutation for tenant {TenantId}", tenantId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                errorCode = DomainErrorCode.Subscription.Blocked.ToString(),
                message = "Your subscription does not allow this operation. Please upgrade or renew your plan."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        if (isBlocked == null)
        {
            _logger.LogError("Subscription gate: unable to verify subscription for tenant {TenantId}. Failing closed.", tenantId);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                errorCode = DomainErrorCode.System.ServiceUnavailable.ToString(),
                message = "Unable to verify subscription status. Please try again."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Returns access info if determined, null if unable to determine (DB/Redis down).
    /// </summary>
    private async Task<SubscriptionAccessEntry?> GetSubscriptionAccessInfo(
        Guid tenantId,
        DateTime now,
        ITenantSubscriptionRepository subscriptionRepository,
        ICacheService cacheService,
        CobryxMetrics metrics)
    {
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        try
        {
            var cached = await cacheService.GetAsync<SubscriptionAccessEntry>(cacheKey);
            if (cached != null)
            {
                if ((now - cached.CheckedAtUtc) > CacheTtl)
                {
                    _logger.LogInformation("Stale cache entry for tenant {TenantId}, falling through to DB", tenantId);
                }
                else
                {
                    metrics.SubscriptionGateCacheHits.Add(1);
                    return cached;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for subscription gate, falling back to DB");
        }

        metrics.SubscriptionGateCacheMisses.Add(1);

        try
        {
            var subscription = await subscriptionRepository.GetByTenantIdAsync(tenantId);

            if (subscription == null)
            {
                var entry = new SubscriptionAccessEntry(true, "unknown", now);
                await TryCacheResult(cacheService, cacheKey, entry);
                return entry;
            }

            var blocked = subscription.IsBlocked(now);
            var tier = subscription.Plan?.Tier.ToString() ?? "unknown";
            var result = new SubscriptionAccessEntry(blocked, tier, now);

            await TryCacheResult(cacheService, cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB unavailable for subscription gate. Cannot verify tenant {TenantId}", tenantId);
            return null;
        }
    }

    private async Task TryCacheResult(ICacheService cacheService, string cacheKey, SubscriptionAccessEntry entry)
    {
        try
        {
            await cacheService.SetAsync(cacheKey, entry, CacheTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache subscription status");
        }
    }

    /// <summary>
    /// Wrapper record to distinguish cache-miss (null) from cached false.
    /// Includes CheckedAtUtc for stale-guard validation.
    /// </summary>
    private sealed record SubscriptionAccessEntry(bool IsBlocked, string Tier, DateTime CheckedAtUtc);
}

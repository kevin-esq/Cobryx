using System.Text.Json;

using Cobryx.Api.Common;
using Cobryx.Api.Filters;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

namespace Cobryx.Api.Middlewares;

public partial class SubscriptionGateMiddleware(RequestDelegate next, ILogger<SubscriptionGateMiddleware> logger)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
    private const string CacheKeyPrefix = "subscription_access:";

    public async Task InvokeAsync(
        HttpContext context,
        ITenantSubscriptionRepository subscriptionRepository,
        ICacheService cacheService,
        CobryxMetrics metrics,
        IClock clock)
    {
        if (ShouldBypass(context))
        {
            await next(context);
            return;
        }

        if (!TryGetTenantId(context, out var tenantId))
        {
            await next(context);
            return;
        }

        var now = clock.UtcNow;
        var accessInfo = await GetSubscriptionAccessInfo(tenantId, now, subscriptionRepository, cacheService, metrics);

        if (accessInfo is not null)
        {
            context.Items["Cache_TenantTier"] = accessInfo.Tier;
        }

        if (accessInfo?.IsBlocked == true)
        {
            metrics.SubscriptionGateBlocked.Add(1,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            LogMutationBlocked(logger, tenantId);

            await WriteResponse(context, StatusCodes.Status403Forbidden,
                DomainErrorCode.Subscription.Blocked,
                "Your subscription does not allow this operation. Please upgrade or renew your plan.");
            return;
        }

        if (accessInfo is null)
        {
            LogVerificationFailed(logger, tenantId);

            await WriteResponse(context, StatusCodes.Status503ServiceUnavailable,
                DomainErrorCode.System.ServiceUnavailable,
                "Unable to verify subscription status. Please try again.");
            return;
        }

        await next(context);
    }

    private static bool ShouldBypass(HttpContext context)
    {
        var method = context.Request.Method;

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<RequiresActiveSubscriptionAttribute>() is null)
            {
                return true;
            }
        }

        var path = context.Request.Path.Value;
        if (path is null)
        {
            return false;
        }

        var normalized = path.ToLowerInvariant();

        return normalized.StartsWith(ApiEndpoints.Health, StringComparison.Ordinal) ||
               normalized.StartsWith(ApiEndpoints.Auth, StringComparison.Ordinal) ||
               normalized.StartsWith(ApiEndpoints.Webhooks, StringComparison.Ordinal) ||
               normalized.StartsWith(ApiEndpoints.Swagger, StringComparison.Ordinal) ||
               normalized.StartsWith(ApiEndpoints.Hangfire, StringComparison.Ordinal) ||
               normalized.StartsWith(ApiEndpoints.Metrics, StringComparison.Ordinal) ||
               normalized.StartsWith(ApiEndpoints.Ping, StringComparison.Ordinal) ||
               normalized == ApiEndpoints.Root ||
               context.GetEndpoint()?.Metadata.GetMetadata<AllowExpiredSubscriptionAttribute>() is not null;
    }

    private static bool TryGetTenantId(HttpContext context, out Guid tenantId)
    {
        if (context.Items.TryGetValue("Cache_TenantId", out var tenantIdObj) &&
            tenantIdObj is Guid id &&
            id != Guid.Empty)
        {
            tenantId = id;
            return true;
        }

        tenantId = default;
        return false;
    }

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
            if (cached is not null)
            {
                if ((now - cached.CheckedAtUtc) <= CacheTtl)
                {
                    metrics.SubscriptionGateCacheHits.Add(1);
                    return cached;
                }

                LogStaleCacheEntry(logger, tenantId);
            }
        }
        catch (Exception ex)
        {
            LogRedisUnavailable(logger, ex);
        }

        metrics.SubscriptionGateCacheMisses.Add(1);

        try
        {
            var subscription = await subscriptionRepository.GetByTenantIdAsync(tenantId);

            var result = subscription is null
                ? new SubscriptionAccessEntry(true, "unknown", now)
                : new SubscriptionAccessEntry(subscription.IsBlocked(now), subscription.Plan.Tier.ToString(), now);

            await TryCacheResult(cacheService, cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            LogDbUnavailable(logger, ex, tenantId);
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
            LogCacheUpdateFailed(logger, ex);
        }
    }

    private static Task WriteResponse(HttpContext context, int statusCode, DomainErrorCode errorCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            errorCode = errorCode.ToString(),
            message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private sealed record SubscriptionAccessEntry(bool IsBlocked, string Tier, DateTime CheckedAtUtc);
}

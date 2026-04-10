using Cobryx.Api.Common;
using Cobryx.Domain.Shared;

using Serilog.Context;

namespace Cobryx.Api.Middlewares;

public class TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TenantMiddleware> _logger = logger;
    private const string TenantHeader = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string? tenantId = null;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claimTenantId = context.User.FindFirst(CobryxClaimTypes.TenantId)?.Value;

            if (context.Request.Headers.TryGetValue(TenantHeader,
                    out Microsoft.Extensions.Primitives.StringValues value) &&
                value != claimTenantId)
            {
                _logger.LogWarning("Security Alert: Tenant mismatch (Token: {TokenId}, Header: {HeaderId})",
                    claimTenantId, value);
            }

            tenantId = claimTenantId;
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            context.Request.Headers.TryGetValue(TenantHeader, out var headerValue);
            tenantId = headerValue;
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith(ApiEndpoints.Health, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ApiEndpoints.Auth, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ApiEndpoints.Webhooks, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ApiEndpoints.Swagger, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ApiEndpoints.Hangfire, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ApiEndpoints.Metrics, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(ApiEndpoints.Ping, StringComparison.OrdinalIgnoreCase) ||
                path == ApiEndpoints.Root || path == "")
            {
                await _next(context);
                return;
            }

            _logger.LogWarning("Request blocked: Tenant context missing for path {Path}", context.Request.Path);
            throw new Domain.Exceptions.Tenants.TenantContextMissingException();
        }

        if (Guid.TryParse(tenantId, out var id))
        {
            context.Items["Cache_TenantId"] = id;
        }

        var userId = context.User.FindFirst("sub")?.Value;
        var correlationId = context.TraceIdentifier;

        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

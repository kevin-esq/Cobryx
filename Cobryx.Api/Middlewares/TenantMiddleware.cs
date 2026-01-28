using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Cobryx.Api.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private const string TenantHeader = "X-Tenant-Id";

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? tenantId = null;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claimTenantId = context.User.FindFirst("tenant_id")?.Value;

            if (context.Request.Headers.ContainsKey(TenantHeader) &&
                context.Request.Headers[TenantHeader] != claimTenantId)
            {
                _logger.LogWarning("Security Alert: Tenant mismatch (Token: {TokenId}, Header: {HeaderId})",
                    claimTenantId, context.Request.Headers[TenantHeader]);
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
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path != null && (path.Contains("/health") || path.Contains("/api/auth")))
            {
                await _next(context);
                return;
            }

            _logger.LogWarning("Request blocked: Tenant context missing for path {Path}", context.Request.Path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Tenant-Id is required.");
            return;
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

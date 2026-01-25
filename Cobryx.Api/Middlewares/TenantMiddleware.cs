using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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

        // 1. If authenticated, get from claims (Most secure)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            tenantId = context.User.FindFirst("tenant_id")?.Value;
        }

        // 2. Fallback to header (For Login/Register)
        if (string.IsNullOrEmpty(tenantId))
        {
            context.Request.Headers.TryGetValue(TenantHeader, out var headerValue);
            tenantId = headerValue;
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            // Allow public paths (Health, Auth) to proceed without tenant if needed
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path != null && (path.Contains("/health") || path.Contains("/api/auth")))
            {
                await _next(context);
                return;
            }

            _logger.LogWarning("Request blocked: Tenant context missing.");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Tenant-Id is required or could not be resolved from token.");
            return;
        }

        // Cache it for the TenantProvider (Request-level cache)
        if (Guid.TryParse(tenantId, out var id))
        {
            context.Items["Cache_TenantId"] = id;
        }

        await _next(context);
    }
}

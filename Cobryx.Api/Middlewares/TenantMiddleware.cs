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
        // For development, we might allow no tenant, but for production it's mandatory
        if (!context.Request.Headers.TryGetValue(TenantHeader, out var tenantIdStr))
        {
            // We could return 400 Bad Request here for mandatory multi-tenancy
            // For now, just log and continue (or uncomment below for strict)
            // context.Response.StatusCode = 400;
            // await context.Response.WriteAsync("Tenant-Id is required.");
            // return;
            
            _logger.LogWarning("Request received without X-Tenant-Id header.");
        }

        await _next(context);
    }
}

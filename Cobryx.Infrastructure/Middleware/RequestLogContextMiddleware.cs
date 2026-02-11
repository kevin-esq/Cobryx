using Serilog.Context;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Middleware;

public class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("X-Correlation-ID", correlationId);
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    LogContext.PushProperty("UserId", userId);
                }
            }

            await _next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }

        return context.TraceIdentifier;
    }
}

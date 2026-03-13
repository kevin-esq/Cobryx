using System.Net;

using Cobryx.Domain.Shared;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Middleware;

public class DynamicRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DynamicRateLimitingMiddleware> _logger;

    public DynamicRateLimitingMiddleware(RequestDelegate next, IDistributedCache cache, ILogger<DynamicRateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/auth/login") ||
            context.Request.Path.StartsWithSegments("/api/mfa/verify"))
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? CobryxDefaults.UnknownValue;
            var cacheKey = $"auth_attempts_{ipAddress}";

            var attemptsStr = await _cache.GetStringAsync(cacheKey);
            if (int.TryParse(attemptsStr, out int attempts) && attempts > 1)
            {
                var delaySeconds = GetDelaySeconds(attempts);

                if (delaySeconds > 0)
                {
                    _logger.LogWarning("Rate limiting active for {IP}. Attempts: {Attempts}. Delaying for {Delay}s", ipAddress, attempts, delaySeconds);

                    var lastAttemptKey = $"last_auth_attempt_{ipAddress}";
                    var lastAttemptStr = await _cache.GetStringAsync(lastAttemptKey);

                    if (DateTime.TryParse(lastAttemptStr, out DateTime lastAttempt))
                    {
                        var timeSinceLast = DateTime.UtcNow - lastAttempt;
                        if (timeSinceLast.TotalSeconds < delaySeconds)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                            context.Response.Headers.Append("Retry-After", ((int)(delaySeconds - timeSinceLast.TotalSeconds)).ToString());
                            await context.Response.WriteAsJsonAsync(new { Error = "Too many failed attempts. Please wait." });
                            return;
                        }
                    }
                }
            }
        }

        await _next(context);
    }

    private static int GetDelaySeconds(int attempts)
    {
        return attempts switch
        {
            < 2 => 0,
            2 => 1,
            3 => 5,
            4 => 15,
            _ => 30
        };
    }
}

using System.Diagnostics;

using Microsoft.Extensions.Primitives;

namespace Cobryx.Api.Middlewares;

/// <summary>
/// Ensures every request has a unique correlation ID (X-Request-Id).
/// Follows the W3C Trace Context recommendation for trace propagation.
/// </summary>
public class RequestIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out StringValues requestId))
        {
            requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        }

        context.Response.Headers.Append(HeaderName, requestId);

        // Ensure the ID is available in the logic and logs
        context.Items[HeaderName] = requestId.ToString();

        await next(context);
    }
}

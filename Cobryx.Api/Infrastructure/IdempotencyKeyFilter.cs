using Microsoft.AspNetCore.Mvc.Filters;

namespace Cobryx.Api.Infrastructure;

/// <summary>
/// Action filter that extracts the Idempotency-Key header from mutation requests
/// and makes it available via HttpContext.Items for downstream use.
/// A missing key on critical POST endpoints is logged as a warning (soft enforcement).
/// </summary>
public class IdempotencyKeyFilter : IActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private readonly ILogger<IdempotencyKeyFilter> _logger;

    public IdempotencyKeyFilter(ILogger<IdempotencyKeyFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
            return;

        var hasKey = context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValue);

        if (hasKey && !string.IsNullOrWhiteSpace(keyValue))
        {
            context.HttpContext.Items["IdempotencyKey"] = keyValue.ToString();
        }
        else
        {
            var path = context.HttpContext.Request.Path.Value ?? "";
            if (IsCriticalMutationEndpoint(path))
            {
                _logger.LogWarning(
                    "POST mutation {Path} missing Idempotency-Key header. TraceId: {TraceId}",
                    path, context.HttpContext.TraceIdentifier);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static bool IsCriticalMutationEndpoint(string path)
    {
        return path.Contains("/loans", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/credits", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/payments", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/invoices", StringComparison.OrdinalIgnoreCase);
    }
}

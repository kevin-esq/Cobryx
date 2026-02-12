using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cobryx.Api.Infrastructure;

/// <summary>
/// Interceptor that enforces at-most-once execution guarantees for critical mutation endpoints.
/// Uses a Redis-backed cache to store and replay full API responses (headers + status + body).
/// </summary>
public class IdempotencyKeyFilter : IAsyncActionFilter
{
    private const string HeaderName = "X-Idempotency-Key";
    private readonly ICacheService _cacheService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<IdempotencyKeyFilter> _logger;

    public IdempotencyKeyFilter(
        ICacheService cacheService,
        ITenantProvider tenantProvider,
        ILogger<IdempotencyKeyFilter> logger)
    {
        _cacheService = cacheService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasAttribute = context.ActionDescriptor.EndpointMetadata.Any(em => em is IdempotentAttribute);

        if (!hasAttribute && !HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValue) ||
            string.IsNullOrWhiteSpace(keyValue))
        {
            if (hasAttribute)
            {
                context.Result = new BadRequestObjectResult(new { error = "X-Idempotency-Key header is required for this action." });
                return;
            }

            await next();
            return;
        }

        var key = keyValue.ToString();
        var tenantId = _tenantProvider.GetTenantId()?.ToString() ?? "global";
        var cacheKey = $"idem:{tenantId}:{key}";

        var cachedResponse = await _cacheService.GetAsync<IdempotencyResponse>(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Idempotency hit for key {Key} in tenant {TenantId}.", key, tenantId);

            var result = new ContentResult
            {
                Content = cachedResponse.Content,
                ContentType = cachedResponse.ContentType,
                StatusCode = cachedResponse.StatusCode
            };

            if (!string.IsNullOrEmpty(cachedResponse.LocationHeader))
            {
                context.HttpContext.Response.Headers.Location = cachedResponse.LocationHeader;
            }

            context.Result = result;
            return;
        }

        var executedContext = await next();

        if (executedContext.Exception == null && executedContext.Result != null)
        {
            var response = await CaptureResponse(executedContext.Result);
            if (response != null && response.StatusCode >= 200 && response.StatusCode < 300)
            {
                if (context.HttpContext.Response.Headers.TryGetValue("Location", out var location))
                {
                    response.LocationHeader = location.ToString();
                }

                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(24));
                _logger.LogInformation("Stored idempotency response for key {Key} in tenant {TenantId}.", key, tenantId);
            }
        }
    }

    private async Task<IdempotencyResponse?> CaptureResponse(IActionResult result)
    {
        int statusCode = 200;
        string? content = null;
        string contentType = "application/json";

        if (result is ObjectResult objectResult)
        {
            statusCode = objectResult.StatusCode ?? 200;
            content = JsonSerializer.Serialize(objectResult.Value);
        }
        else if (result is StatusCodeResult statusCodeResult)
        {
            statusCode = statusCodeResult.StatusCode;
        }
        else if (result is ContentResult contentResult)
        {
            statusCode = contentResult.StatusCode ?? 200;
            content = contentResult.Content;
            contentType = contentResult.ContentType ?? "text/plain";
        }
        else
        {
            return null;
        }

        return new IdempotencyResponse
        {
            StatusCode = statusCode,
            Content = content,
            ContentType = contentType
        };
    }

    private class IdempotencyResponse
    {
        public int StatusCode { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public string? LocationHeader { get; set; }
    }
}

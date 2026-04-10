using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Idempotency;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Cobryx.Api.Filters;

/// <summary>
/// Interceptor that enforces at-most-once execution guarantees for critical mutation endpoints.
/// Uses Redis fast-path + DB persistence for durability across Redis restarts.
/// Validates request hash to detect payload reuse attacks.
/// </summary>
public partial class IdempotencyKeyFilter(
    IIdempotencyStore idempotencyStore,
    ITenantProvider tenantProvider,
    ILogger<IdempotencyKeyFilter> logger) : IAsyncActionFilter
{
    private const string HeaderName = "X-Idempotency-Key";
    private static readonly TimeSpan _defaultTtl = TimeSpan.FromHours(24);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasAttribute = context.ActionDescriptor.EndpointMetadata.Any(static em => em is IdempotentAttribute);

        if (!hasAttribute && !HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out StringValues keyValue) ||
            string.IsNullOrWhiteSpace(keyValue))
        {
            if (hasAttribute)
            {
                context.Result = new BadRequestObjectResult(new
                { error = "X-Idempotency-Key header is required for this action." });
                return;
            }

            await next();
            return;
        }

        var key = keyValue.ToString();
        var tenantId = tenantProvider.GetTenantId() ?? Guid.Empty;

        // Compute request hash for payload validation
        string requestHash = ComputeRequestHash(context);

        // Try to acquire idempotency lock (Redis + DB)
        var (acquired, existing) = await idempotencyStore.TryAcquireAsync(
            tenantId, key, requestHash, _defaultTtl, context.HttpContext.RequestAborted);

        if (!acquired && existing != null)
        {
            // Check if request hash matches (same payload)
            if (!existing.RequestMatches(requestHash))
            {
                LogIdempotencyHashMismatch(logger, key, tenantId.ToString());
                context.Result = new ConflictObjectResult(new
                {
                    error = "Idempotency key already used with different request payload.",
                    code = "IDEMPOTENCY_KEY_REUSED"
                });
                return;
            }

            // Request in progress - return 409 Conflict
            if (existing.Status == IdempotencyStatus.Processing)
            {
                LogIdempotencyInProgress(logger, key, tenantId.ToString());
                context.Result = new ConflictObjectResult(new
                {
                    error = "Request with this idempotency key is currently being processed.",
                    code = "IDEMPOTENCY_IN_PROGRESS"
                });
                return;
            }

            // Completed - return cached response
            if (existing.Status == IdempotencyStatus.Completed)
            {
                LogIdempotencyHit(logger, key, tenantId.ToString());

                var result = new ContentResult
                {
                    Content = existing.ResponseBody,
                    ContentType = existing.ContentType ?? "application/json",
                    StatusCode = existing.StatusCode
                };

                if (!string.IsNullOrEmpty(existing.LocationHeader))
                {
                    context.HttpContext.Response.Headers.Location = existing.LocationHeader;
                }

                context.Result = result;
                return;
            }
        }

        // Set idempotency context for handlers that want to complete within their transaction
        idempotencyStore.SetCurrentContext(tenantId, key);

        // Execute the action
        ActionExecutedContext executedContext;
        try
        {
            executedContext = await next();
        }
        catch (Exception ex)
        {
            // Mark as failed on exception (allows retry)
            await idempotencyStore.FailAsync(tenantId, key, context.HttpContext.RequestAborted);
            LogIdempotencyFailed(logger, key, tenantId.ToString(), ex);
            throw;
        }

        // Check if handler already completed idempotency within its transaction
        var idempotencyContext = idempotencyStore.GetCurrentContext();
        var completedInTransaction = idempotencyContext?.IsCompleted ?? false;

        // Capture and store response
        if (executedContext is { Exception: null, Result: not null })
        {
            var response = CaptureResponse(executedContext.Result);

            // Only cache non-5xx responses
            if (response != null && response.StatusCode < 500)
            {
                string? locationHeader = null;
                if (context.HttpContext.Response.Headers.TryGetValue("Location", out StringValues location))
                {
                    locationHeader = location.ToString();
                }

                // Skip if already completed within handler's transaction (EXACTLY-ONCE)
                if (!completedInTransaction)
                {
                    await idempotencyStore.CompleteAsync(
                        tenantId,
                        key,
                        response.StatusCode,
                        response.Content,
                        response.ContentType,
                        locationHeader,
                        context.HttpContext.RequestAborted);
                }

                LogIdempotencyStored(logger, key, tenantId.ToString());
            }
            else
            {
                // 5xx error - mark as failed to allow retry
                await idempotencyStore.FailAsync(tenantId, key, context.HttpContext.RequestAborted);
            }
        }
        else if (executedContext.Exception != null)
        {
            // Exception occurred - mark as failed
            await idempotencyStore.FailAsync(tenantId, key, context.HttpContext.RequestAborted);
        }
    }

    /// <summary>
    /// Computes a normalized hash of the request for payload validation.
    /// Handles JSON normalization to ensure consistent hashing regardless of property order.
    /// </summary>
    private static string ComputeRequestHash(ActionExecutingContext context)
    {
        // For requests without body, hash the action arguments
        if (context.ActionArguments.Count > 0)
        {
            // Serialize with sorted keys for consistent hashing
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            // Sort and serialize action arguments for deterministic hash
            var sortedArgs = context.ActionArguments
                .OrderBy(static kvp => kvp.Key, StringComparer.Ordinal)
                .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);

            string json = JsonSerializer.Serialize(sortedArgs, options);
            return ComputeSha256Hash(json);
        }

        // Fallback: hash empty string for requests without arguments
        return ComputeSha256Hash(string.Empty);
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static CapturedResponse? CaptureResponse(IActionResult result)
    {
        int statusCode;
        string? content = null;
        var contentType = "application/json";

        switch (result)
        {
            case ObjectResult objectResult:
                statusCode = objectResult.StatusCode ?? 200;
                content = JsonSerializer.Serialize(objectResult.Value);
                break;
            case StatusCodeResult statusCodeResult:
                statusCode = statusCodeResult.StatusCode;
                break;
            case ContentResult contentResult:
                statusCode = contentResult.StatusCode ?? 200;
                content = contentResult.Content;
                contentType = contentResult.ContentType ?? "text/plain";
                break;
            default:
                return null;
        }

        return new CapturedResponse(statusCode, content, contentType);
    }

    private sealed record CapturedResponse(int StatusCode, string? Content, string? ContentType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency hit for key {Key} tenant {TenantId}")]
    private static partial void LogIdempotencyHit(ILogger logger, string key, string tenantId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency stored for key {Key} tenant {TenantId}")]
    private static partial void LogIdempotencyStored(ILogger logger, string key, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency key {Key} reused with different payload for tenant {TenantId}")]
    private static partial void LogIdempotencyHashMismatch(ILogger logger, string key, string tenantId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotency key {Key} is currently processing for tenant {TenantId}")]
    private static partial void LogIdempotencyInProgress(ILogger logger, string key, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency key {Key} failed for tenant {TenantId}")]
    private static partial void LogIdempotencyFailed(ILogger logger, string key, string tenantId, Exception ex);
}

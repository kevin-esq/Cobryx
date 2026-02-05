using Cobryx.Api.Errors;
using Cobryx.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionFeature = httpContext.Features.Get<IExceptionHandlerFeature>();

        var targetException = exception ?? exceptionFeature?.Error;

        if (targetException == null) return true;

        string errorCode;
        Dictionary<string, object>? metadata = null;

        if (targetException is CobryxException cobryxEx)
        {
            errorCode = cobryxEx.ErrorCode;
            metadata = cobryxEx.Metadata;
        }
        else if (targetException is FluentValidation.ValidationException validationEx)
        {
            errorCode = "VALIDATION.FAILED";
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            metadata = new Dictionary<string, object> { { "errors", errors } };
        }
        else
        {
            errorCode = "SYSTEM.INTERNAL_ERROR";
        }

        var (statusCode, numericCode, title) = ErrorMapper.Map(errorCode);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(targetException, "Unhandled exception occurred: {Message} [Code: {NumericCode}]", targetException.Message, numericCode);
        }
        else
        {
            _logger.LogWarning("Application exception: {Message} [Code: {NumericCode}]", targetException.Message, numericCode);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == 500 ? "An unexpected error occurred." : targetException.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = errorCode;
        problemDetails.Extensions["numericCode"] = numericCode;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (metadata != null && metadata.Count > 0)
        {
            foreach (var item in metadata)
            {
                problemDetails.Extensions[item.Key] = item.Value;
            }
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

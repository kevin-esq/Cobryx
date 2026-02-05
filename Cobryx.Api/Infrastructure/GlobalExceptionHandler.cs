using Cobryx.Api.Errors.Mappers;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Application.Common.Models;
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
            errorCode = DomainErrorCodes.System.ValidationFailed;
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => new
                    {
                        errorCode = e.ErrorCode,
                        @params = e.FormattedMessagePlaceholderValues
                    }).ToArray());

            metadata = new Dictionary<string, object> { { "errors", errors } };
        }
        else
        {
            errorCode = DomainErrorCodes.System.InternalError;
        }

        var errorDef = ErrorMapper.Map(errorCode);
        var statusCode = errorDef.StatusCode;
        var numericCode = errorDef.NumericCode;

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(targetException, "Unhandled exception occurred: {Message} [Code: {NumericCode}]", targetException.Message, numericCode);
        }
        else
        {
            _logger.LogWarning("Application exception: {Message} [Code: {NumericCode}]", targetException.Message, numericCode);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: numericCode,
            errors: metadata?.GetValueOrDefault("errors"),
            traceId: httpContext.TraceIdentifier);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}

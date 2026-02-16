using Cobryx.Api.Errors.Mappers;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Application.Common.Models;
using Cobryx.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Application.Common.Observability;
using Serilog;

namespace Cobryx.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly CobryxMetrics _metrics;
    private readonly IDiagnosticContext _diagnosticContext;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        CobryxMetrics metrics,
        IDiagnosticContext diagnosticContext)
    {
        _logger = logger;
        _metrics = metrics;
        _diagnosticContext = diagnosticContext;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionFeature = httpContext.Features.Get<IExceptionHandlerFeature>();

        var targetException = exception ?? exceptionFeature?.Error;

        if (targetException == null) return true;

        DomainErrorCode errorCode;
        Dictionary<string, object>? metadata = null;
        IReadOnlyList<ValidationError>? structuredErrors = null;

        if (targetException is CobryxException cobryxEx)
        {
            errorCode = cobryxEx.ErrorCode;
            metadata = cobryxEx.Metadata;
        }
        else if (targetException is FluentValidation.ValidationException validationEx)
        {
            errorCode = DomainErrorCode.System.ValidationFailed;
            structuredErrors = validationEx.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorCode, e.ErrorMessage))
                .ToList();

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
            errorCode = DomainErrorCode.System.InternalError;
        }

        var errorDef = ErrorMapper.Map(errorCode);
        var statusCode = errorDef.StatusCode;
        var numericCode = errorDef.NumericCode;

        var outcomeCode = GetFailedOutcomeCode(errorCode.Value);

        _diagnosticContext.Set("ErrorCode", errorCode.Value);
        _diagnosticContext.Set("NumericCode", numericCode);
        _diagnosticContext.Set("OutcomeCode", outcomeCode);
        _metrics.RecordError(errorCode.Value, numericCode);
        _metrics.RecordOutcome(outcomeCode);

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

        if (structuredErrors == null)
        {
            var rawErrors = metadata?.GetValueOrDefault("errors");
            if (rawErrors != null)
            {
                structuredErrors = new[] { new ValidationError("_global", "DOMAIN_ERROR", rawErrors.ToString() ?? string.Empty) };
            }
        }

        var response = Cobryx.Api.Contracts.V1.Common.ApiResponseFactory.Error(
            errorCode: errorCode.Value,
            numericCode: numericCode,
            errors: structuredErrors,
            traceId: httpContext.TraceIdentifier,
            outcomeCode: outcomeCode);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static string GetFailedOutcomeCode(string errorCode)
    {
        if (string.IsNullOrEmpty(errorCode)) return "SYSTEM.FAILED";
        if (errorCode.EndsWith(".FAILED")) return errorCode;

        var parts = errorCode.Split('.');
        return $"{parts[0]}.FAILED";
    }
}

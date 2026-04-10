using Cobryx.Api.Errors.Mappers;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Shared;

using Microsoft.AspNetCore.Diagnostics;

using Serilog;

namespace Cobryx.Api.Filters;

public partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    CobryxMetrics metrics,
    IDiagnosticContext diagnosticContext) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (errorCode, metadata, structuredErrors) = MapException(exception);

        var errorDef = ErrorMapper.Map(errorCode);
        var statusCode = errorDef.StatusCode;
        var numericCode = errorDef.NumericCode;

        var outcome = GetFailedOutcomeCode(errorCode.Value);

        diagnosticContext.Set("ErrorCode", errorCode.Value);
        diagnosticContext.Set("NumericCode", numericCode);
        diagnosticContext.Set("OutcomeCode", outcome);

        metrics.RecordError(errorCode.Value, numericCode);
        metrics.RecordOutcome(
            outcome.Value,
            false,
            context.Items["Cache_TenantTier"] as string,
            statusCode.ToString());

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, exception.Message, numericCode);
        }
        else
        {
            LogApplicationException(logger, exception.Message, numericCode);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errors = EnsureStructuredErrors(structuredErrors, metadata);

        var response = ApiResponseFactory.Error(
            errorCode: errorCode.Value,
            numericCode: numericCode,
            errors: errors,
            traceId: context.TraceIdentifier,
            outcomeCode: outcome);

        await context.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static (DomainErrorCode errorCode, Dictionary<string, object>? metadata, IReadOnlyList<ValidationError>?
        structuredErrors)
        MapException(Exception exception)
    {
        return exception switch
        {
            CobryxException ex => (ex.ErrorCode, ex.Metadata, null),

            FluentValidation.ValidationException validationEx => (
                DomainErrorCode.System.ValidationFailed,
                BuildValidationMetadata(validationEx),
                BuildValidationErrors(validationEx)
            ),

            _ => (DomainErrorCode.System.InternalError, null, null)
        };
    }

    private static IReadOnlyList<ValidationError> BuildValidationErrors(FluentValidation.ValidationException ex)
    {
        return
        [
            .. ex.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorCode, e.ErrorMessage))
        ];
    }

    private static Dictionary<string, object> BuildValidationMetadata(FluentValidation.ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => new
                {
                    errorCode = e.ErrorCode,
                    @params = e.FormattedMessagePlaceholderValues
                }).Cast<object>().ToArray());

        return new Dictionary<string, object> { { "errors", errors } };
    }

    private static IReadOnlyList<ValidationError>? EnsureStructuredErrors(
        IReadOnlyList<ValidationError>? structuredErrors,
        Dictionary<string, object>? metadata)
    {
        if (structuredErrors is not null)
        {
            return structuredErrors;
        }

        if (metadata?.TryGetValue("errors", out var raw) == true)
        {
            return
            [
                new ValidationError("_global", ValidationCodes.DomainError, raw.ToString() ?? string.Empty)
            ];
        }

        return null;
    }

    private static Outcome GetFailedOutcomeCode(string errorCode)
    {
        const string systemFailed = "SYSTEM.FAILED";
        const string failedSuffix = ".FAILED";

        if (string.IsNullOrEmpty(errorCode))
        {
            return Outcome.FromExternal(systemFailed, OutcomeCategory.Critical);
        }

        if (errorCode.EndsWith(failedSuffix))
        {
            return Outcome.FromExternal(errorCode);
        }

        var separatorIndex = errorCode.IndexOf('.');
        var prefix = separatorIndex > 0 ? errorCode[..separatorIndex] : errorCode;

        return Outcome.FromExternal($"{prefix}{failedSuffix}");
    }
}

using Cobryx.Application.Common.Models;
using Cobryx.Infrastructure.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace Cobryx.Api.Infrastructure.Observability;

public class ObservabilityFilter : IAsyncActionFilter
{
    private readonly IDiagnosticContext _diagnosticContext;
    private readonly CobryxMetrics _metrics;

    public ObservabilityFilter(IDiagnosticContext diagnosticContext, CobryxMetrics metrics)
    {
        _diagnosticContext = diagnosticContext;
        _metrics = metrics;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();

        string? outcomeCode = null;
        string? errorCode = null;
        int? numericCode = null;

        if (executedContext.Result is ObjectResult objectResult && objectResult.Value is ApiResponse response)
        {
            if (response.Success)
            {
                outcomeCode = response.OutcomeCode;
            }
            else if (response is ApiErrorResponse errorResponse)
            {
                errorCode = errorResponse.ErrorCode;
                numericCode = errorResponse.NumericCode;
                if (!string.IsNullOrEmpty(errorResponse.OutcomeCode))
                {
                    outcomeCode = errorResponse.OutcomeCode;
                }
            }
        }

        if (string.IsNullOrEmpty(outcomeCode))
        {
            if (executedContext.Exception != null || (executedContext.Result is ObjectResult or && or.StatusCode >= 400) || (executedContext.Result is StatusCodeResult sr && sr.StatusCode >= 400))
            {
                outcomeCode = !string.IsNullOrEmpty(errorCode) ? GetFailedOutcomeCode(errorCode) : "SYSTEM.OPERATION.FAILED";
            }
            else
            {
                outcomeCode = "SYSTEM.OPERATION.SUCCESS";
            }
        }

        _diagnosticContext.Set("OutcomeCode", outcomeCode);
        _metrics.RecordOutcome(outcomeCode);

        if (!string.IsNullOrEmpty(errorCode))
        {
            _diagnosticContext.Set("ErrorCode", errorCode);
            _metrics.RecordError(errorCode, numericCode);
        }
    }

    private static string GetFailedOutcomeCode(string errorCode)
    {
        if (string.IsNullOrEmpty(errorCode)) return "SYSTEM.FAILED";
        if (errorCode.EndsWith(".FAILED")) return errorCode;

        var parts = errorCode.Split('.');
        return $"{parts[0]}.FAILED";
    }
}

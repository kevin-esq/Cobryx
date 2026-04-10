using Cobryx.Application.Common.Observability;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Serilog;

namespace Cobryx.Api.Filters.Observability;

public class ObservabilityFilter(IDiagnosticContext diagnosticContext, CobryxMetrics metrics) : IAsyncActionFilter
{
    private readonly IDiagnosticContext _diagnosticContext = diagnosticContext;
    private readonly CobryxMetrics _metrics = metrics;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();

        string? outcomeCode = null;
        string? errorCode = null;
        int? numericCode = null;

        if (executedContext.Result is ObjectResult objectResult && objectResult.Value is Cobryx.Api.Contracts.V1.Common.ApiResponse response)
        {
            if (response.Success)
            {
                outcomeCode = response.OutcomeCode;
            }
            else if (response is Cobryx.Api.Contracts.V1.Common.ApiErrorResponse errorResponse)
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
            if (executedContext.Exception != null || (executedContext.Result is ObjectResult orRes && orRes.StatusCode >= 400) || (executedContext.Result is StatusCodeResult scRes && scRes.StatusCode >= 400))
            {
                outcomeCode = !string.IsNullOrEmpty(errorCode) ? GetFailedOutcomeCode(errorCode) : "SYSTEM.OPERATION.FAILED";
            }
            else
            {
                outcomeCode = "SYSTEM.OPERATION.SUCCESS";
            }
        }

        var tier = context.HttpContext.Items["Cache_TenantTier"] as string;
        var isSuccess = executedContext.Exception == null &&
                        (executedContext.Result is not ObjectResult objRes || objRes.StatusCode is null or < 400);

        var status = (executedContext.Result as ObjectResult)?.StatusCode ??
                     (executedContext.Result is StatusCodeResult statRes ? statRes.StatusCode : 200);

        _diagnosticContext.Set("OutcomeCode", outcomeCode);
        _diagnosticContext.Set("Tier", tier ?? "unknown");
        _diagnosticContext.Set("StatusCode", status);

        _metrics.RecordOutcome(outcomeCode, isSuccess, tier, status.ToString());

        if (!string.IsNullOrEmpty(errorCode))
        {
            _diagnosticContext.Set("ErrorCode", errorCode);
            _metrics.RecordError(errorCode, numericCode);
        }
    }

    private static string GetFailedOutcomeCode(string errorCode)
    {
        if (string.IsNullOrEmpty(errorCode))
            return "SYSTEM.FAILED";
        if (errorCode.EndsWith(".FAILED"))
            return errorCode;

        var parts = errorCode.Split('.');
        return $"{parts[0]}.FAILED";
    }
}

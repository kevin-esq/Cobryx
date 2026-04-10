using Cobryx.Api.Errors.Definitions;
using Cobryx.Api.Errors.Mappers;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class CobryxBaseController(ISender sender) : ControllerBase
{
    protected readonly ISender Sender = sender;

    protected IActionResult HandleResult<T>(Result<T> result, Outcome? outcomeCode = null,
        int? successStatusCode = null)
    {
        if (result.IsSuccess)
        {
            ApiSuccessResponse<T> response = ApiResponseFactory.Success(result.Value!, outcomeCode);
            return successStatusCode.HasValue
                ? StatusCode(successStatusCode.Value, response)
                : Ok(response);
        }

        DomainErrorCode errorCode = result.Error ?? DomainErrorCode.Common.GeneralError;
        ErrorDefinition errorDef = ErrorMapper.Map(DomainErrorCode.From(errorCode));

        var statusCode = GetErrorStatusCode(errorCode, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: GetFailureOutcomeCode(outcomeCode)));
    }

    protected IActionResult HandleResult(Result result, Outcome? outcomeCode = null,
        int? successStatusCode = null)
    {
        if (result.IsSuccess)
        {
            ApiSuccessResponse response = ApiResponseFactory.Success(outcomeCode: outcomeCode);
            return successStatusCode.HasValue
                ? StatusCode(successStatusCode.Value, response)
                : Ok(response);
        }

        DomainErrorCode errorCode = result.Error ?? DomainErrorCode.Common.GeneralError;
        ErrorDefinition errorDef = ErrorMapper.Map(DomainErrorCode.From(errorCode));

        var statusCode = GetErrorStatusCode(errorCode, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: GetFailureOutcomeCode(outcomeCode)));
    }

    protected IActionResult Success<T>(T data, Outcome? outcomeCode = null) =>
        Ok(ApiResponseFactory.Success(data, outcomeCode));

    protected IActionResult HandleDeleteResult(Result result, Outcome? outcomeCode = null)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        DomainErrorCode errorCode = result.Error ?? DomainErrorCode.Common.GeneralError;
        ErrorDefinition errorDef = ErrorMapper.Map(DomainErrorCode.From(errorCode));
        var statusCode = GetErrorStatusCode(errorCode, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: GetFailureOutcomeCode(outcomeCode)));
    }

    protected IActionResult CreatedResult<T>(string uri, T data, Outcome? outcomeCode = null) =>
        Created(uri, ApiResponseFactory.Success(data, outcomeCode));

    protected IActionResult HandleCreatedResult<T>(string? uri, Result<T> result, Outcome? outcomeCode = null)
    {
        if (!result.IsSuccess)
        {
            return HandleResult(result, outcomeCode);
        }

        ApiSuccessResponse<T> response = ApiResponseFactory.Success(result.Value!, outcomeCode);
        return string.IsNullOrEmpty(uri)
            ? StatusCode(201, response)
            : Created(uri, response);
    }

    private static int GetErrorStatusCode(string errorCode, int defaultStatusCode)
    {
        if (errorCode.EndsWith(".ALREADY_EXISTS") || errorCode.EndsWith(".DUPLICATE"))
            return 409;
        if (errorCode.EndsWith(".NOT_FOUND"))
            return 404;
        if (errorCode.EndsWith(".BUSINESS_RULE_VIOLATION") || errorCode.Contains(".INVALID_STATUS"))
            return 422;

        return defaultStatusCode;
    }

    private static Outcome GetFailureOutcomeCode(Outcome? successOutcome)
    {
        if (successOutcome == null)
            return Outcome.FromExternal("SYSTEM.OPERATION.FAILED");

        var val = successOutcome.Value;
        var lastDot = val.LastIndexOf('.');
        if (lastDot > 0)
        {
            return Outcome.FromExternal(string.Concat(val.AsSpan(0, lastDot), ".FAILED"));
        }

        return Outcome.FromExternal($"{val}.FAILED");
    }
}

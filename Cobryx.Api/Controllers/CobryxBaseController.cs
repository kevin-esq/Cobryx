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

    protected IActionResult HandleResult<T>(Result<T> result, Outcome? outcomeCode = null, int? successStatusCode = null) => result.IsSuccess ? RespondWithSuccess(result.Value!, outcomeCode, successStatusCode) : RespondWithError(result.Error, outcomeCode);

    protected IActionResult HandleResult(Result result, Outcome? outcomeCode = null, int? successStatusCode = null) => result.IsSuccess ? RespondWithSuccess(outcomeCode, successStatusCode) : RespondWithError(result.Error, outcomeCode);

    protected IActionResult HandleCreatedResult<T>(string? uri, Result<T> result, Outcome? outcomeCode = null)
    {
        if (!result.IsSuccess)
            return RespondWithError(result.Error, outcomeCode);

        ApiSuccessResponse<T> response = BuildSuccessResponse(result.Value!, outcomeCode);

        return string.IsNullOrEmpty(uri)
            ? StatusCode(StatusCodes.Status201Created, response)
            : Created(uri, response);
    }

    protected IActionResult HandleDeleteResult(Result result, Outcome? outcomeCode = null)
    {
        if (result.IsSuccess)
            return NoContent();

        return RespondWithError(result.Error, outcomeCode);
    }

    protected IActionResult HandleAcceptedResult(Result result, Outcome? outcomeCode = null)
    {
        if (!result.IsSuccess)
            return RespondWithError(result.Error, outcomeCode);

        return Accepted(ApiResponseFactory.Success(outcomeCode: outcomeCode, traceId: HttpContext.TraceIdentifier));
    }

    protected IActionResult Success<T>(T data, Outcome? outcomeCode = null) =>
        Ok(ApiResponseFactory.Success(data, outcomeCode, HttpContext.TraceIdentifier));

    protected IActionResult CreatedResult<T>(string uri, T data, Outcome? outcomeCode = null) =>
        Created(uri, ApiResponseFactory.Success(data, outcomeCode, HttpContext.TraceIdentifier));

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private IActionResult RespondWithSuccess<T>(T data, Outcome? outcomeCode, int? statusCode)
    {
        ApiSuccessResponse<T> response = BuildSuccessResponse(data, outcomeCode);
        return statusCode.HasValue ? StatusCode(statusCode.Value, response) : Ok(response);
    }

    private IActionResult RespondWithSuccess(Outcome? outcomeCode, int? statusCode)
    {
        ApiSuccessResponse response = ApiResponseFactory.Success(outcomeCode: outcomeCode, traceId: HttpContext.TraceIdentifier);
        return statusCode.HasValue ? StatusCode(statusCode.Value, response) : Ok(response);
    }

    private IActionResult RespondWithError(DomainErrorCode? errorCode, Outcome? outcomeCode)
    {
        DomainErrorCode code = errorCode ?? DomainErrorCode.Common.GeneralError;
        ErrorDefinition errorDef = ErrorMapper.Map(DomainErrorCode.From(code));
        var statusCode = ResolveErrorStatusCode(code, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: code,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: BuildFailureOutcome(outcomeCode)));
    }

    private ApiSuccessResponse<T> BuildSuccessResponse<T>(T data, Outcome? outcomeCode) =>
        ApiResponseFactory.Success(data, outcomeCode, HttpContext.TraceIdentifier);

    private static int ResolveErrorStatusCode(string errorCode, int defaultStatusCode) => errorCode switch
    {
        _ when errorCode.EndsWith(".ALREADY_EXISTS") || errorCode.EndsWith(".DUPLICATE")         => StatusCodes.Status409Conflict,
        _ when errorCode.EndsWith(".NOT_FOUND")                                                   => StatusCodes.Status404NotFound,
        _ when errorCode.EndsWith(".BUSINESS_RULE_VIOLATION") || errorCode.Contains(".INVALID_STATUS") => StatusCodes.Status422UnprocessableEntity,
        _                                                                                          => defaultStatusCode
    };

    private static Outcome BuildFailureOutcome(Outcome? successOutcome)
    {
        if (successOutcome is null)
            return Outcome.FromExternal("SYSTEM.OPERATION.FAILED");

        ReadOnlySpan<char> val = successOutcome.Value.AsSpan();
        var lastDot = val.LastIndexOf('.');

        var failureCode = lastDot > 0
            ? string.Concat(val[..lastDot], ".FAILED")
            : $"{val}.FAILED";

        return Outcome.FromExternal(failureCode);
    }
}

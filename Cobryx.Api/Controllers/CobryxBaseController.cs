using Cobryx.Api.Errors.Mappers;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Api.Contracts.V1.Common;
using Concordia;
using Cobryx.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class CobryxBaseController : ControllerBase
{
    protected readonly ISender Sender;

    protected CobryxBaseController(ISender sender)
    {
        Sender = sender;
    }

    protected IActionResult HandleResult<T>(Result<T> result, string? outcomeCode = null, int? successStatusCode = null)
    {
        if (result.IsSuccess)
        {
            var response = ApiResponseFactory.Success(result.Value!, outcomeCode);
            return successStatusCode.HasValue
                ? StatusCode(successStatusCode.Value, response)
                : Ok(response);
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(DomainErrorCode.From(errorCode));

        var statusCode = GetErrorStatusCode(errorCode, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: GetFailureOutcomeCode(outcomeCode)));
    }

    protected IActionResult HandleResult(Result result, string? outcomeCode = null, int? successStatusCode = null)
    {
        if (result.IsSuccess)
        {
            var response = ApiResponseFactory.Success(outcomeCode: outcomeCode);
            return successStatusCode.HasValue
                ? StatusCode(successStatusCode.Value, response)
                : Ok(response);
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(DomainErrorCode.From(errorCode));

        var statusCode = GetErrorStatusCode(errorCode, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: GetFailureOutcomeCode(outcomeCode)));
    }

    protected IActionResult Success<T>(T data, string? outcomeCode = null)
    {
        return Ok(ApiResponseFactory.Success(data, outcomeCode));
    }

    protected IActionResult HandleDeleteResult(Result result, string? outcomeCode = null)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(DomainErrorCode.From(errorCode));
        var statusCode = GetErrorStatusCode(errorCode, errorDef.StatusCode);

        return StatusCode(statusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier,
            outcomeCode: GetFailureOutcomeCode(outcomeCode)));
    }

    protected IActionResult CreatedResult<T>(string uri, T data, string? outcomeCode = null)
    {
        return Created(uri, ApiResponseFactory.Success(data, outcomeCode));
    }

    protected IActionResult HandleCreatedResult<T>(string uri, Result<T> result, string? outcomeCode = null)
    {
        return HandleResult(result, outcomeCode, 201);
    }

    private int GetErrorStatusCode(string errorCode, int defaultStatusCode)
    {
        if (errorCode.EndsWith(".ALREADY_EXISTS") || errorCode.EndsWith(".DUPLICATE")) return 409;
        if (errorCode.EndsWith(".NOT_FOUND")) return 404;
        if (errorCode.EndsWith(".BUSINESS_RULE_VIOLATION") || errorCode.Contains(".INVALID_STATUS")) return 422;

        return defaultStatusCode;
    }

    private string GetFailureOutcomeCode(string? successOutcomeCode)
    {
        if (string.IsNullOrEmpty(successOutcomeCode)) return "SYSTEM.OPERATION.FAILED";

        var lastDot = successOutcomeCode.LastIndexOf('.');
        if (lastDot > 0)
        {
            return successOutcomeCode.Substring(0, lastDot) + ".FAILED";
        }

        return $"{successOutcomeCode}.FAILED";
    }
}

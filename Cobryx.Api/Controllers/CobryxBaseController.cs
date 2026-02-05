using Cobryx.Api.Errors.Mappers;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Application.Common.Models;
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

    protected IActionResult HandleResult<T>(Result<T> result, string? outcomeCode = null)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponseFactory.Success(result.Value!, outcomeCode));
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(errorCode);

        return StatusCode(errorDef.StatusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier));
    }

    protected IActionResult HandleResult(Result result, string? outcomeCode = null)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponseFactory.Success(outcomeCode: outcomeCode));
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(errorCode);

        return StatusCode(errorDef.StatusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier));
    }

    protected IActionResult Success<T>(T data, string? outcomeCode = null)
    {
        return Ok(ApiResponseFactory.Success(data, outcomeCode));
    }

    protected IActionResult CreatedResult<T>(string uri, T data, string? outcomeCode = null)
    {
        return Created(uri, ApiResponseFactory.Success(data, outcomeCode));
    }

    protected IActionResult HandleCreatedResult<T>(string uri, Result<T> result, string? outcomeCode = null)
    {
        if (result.IsSuccess)
        {
            return Created(uri, ApiResponseFactory.Success(result.Value!, outcomeCode));
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(errorCode);

        return StatusCode(errorDef.StatusCode, ApiResponseFactory.Error(
            errorCode: errorCode,
            numericCode: errorDef.NumericCode,
            traceId: HttpContext.TraceIdentifier));
    }
}

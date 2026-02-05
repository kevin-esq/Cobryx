using Cobryx.Api.Errors;
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

    protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "Operation completed successfully", string? code = null)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<T>.SuccessResponse(result.Value!, successMessage, code));
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(errorCode);

        return StatusCode(errorDef.StatusCode, new ProblemDetails
        {
            Status = errorDef.StatusCode,
            Title = errorDef.Title,
            Detail = errorCode,
            Instance = HttpContext.Request.Path,
            Extensions = {
                ["code"] = errorCode,
                ["numericCode"] = errorDef.NumericCode,
                ["traceId"] = HttpContext.TraceIdentifier
            }
        });
    }

    protected IActionResult HandleResult(Result result, string successMessage = "Operation completed successfully", string? code = null)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse.SuccessResponse(successMessage, code));
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(errorCode);

        return StatusCode(errorDef.StatusCode, new ProblemDetails
        {
            Status = errorDef.StatusCode,
            Title = errorDef.Title,
            Detail = errorCode,
            Instance = HttpContext.Request.Path,
            Extensions = {
                ["code"] = errorCode,
                ["numericCode"] = errorDef.NumericCode,
                ["traceId"] = HttpContext.TraceIdentifier
            }
        });
    }

    protected IActionResult Success<T>(T data, string message = "Success", string? code = null)
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message, code));
    }

    protected IActionResult CreatedResult<T>(string uri, T data, string message = "Resource created successfully", string? code = null)
    {
        return Created(uri, ApiResponse<T>.SuccessResponse(data, message, code));
    }

    protected IActionResult HandleCreatedResult<T>(string uri, Result<T> result, string successMessage = "Resource created successfully", string? code = null)
    {
        if (result.IsSuccess)
        {
            return Created(uri, ApiResponse<T>.SuccessResponse(result.Value!, successMessage, code));
        }

        var errorCode = result.Error ?? "DOMAIN.GENERAL_ERROR";
        var errorDef = ErrorMapper.Map(errorCode);

        return StatusCode(errorDef.StatusCode, new ProblemDetails
        {
            Status = errorDef.StatusCode,
            Title = errorDef.Title,
            Detail = errorCode,
            Extensions = {
                ["code"] = errorCode,
                ["numericCode"] = errorDef.NumericCode,
                ["traceId"] = HttpContext.TraceIdentifier
            }
        });
    }
}

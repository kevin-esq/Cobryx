using Cobryx.Application.Common.Models;
using Concordia;
using Cobryx.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[ApiController]
public abstract class CobryxBaseController : ControllerBase
{
    protected readonly ISender Sender;

    protected CobryxBaseController(ISender sender)
    {
        Sender = sender;
    }

    protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "Operation completed successfully")
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<T>.SuccessResponse(result.Value!, successMessage));
        }

        return BadRequest(ApiResponse<T>.FailureResponse(result.Error ?? "An error occurred", null, HttpContext.TraceIdentifier));
    }

    protected IActionResult HandleResult(Result result, string successMessage = "Operation completed successfully")
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse.SuccessResponse(successMessage));
        }

        return BadRequest(ApiResponse.FailureResponse(result.Error ?? "An error occurred", null, HttpContext.TraceIdentifier));
    }

    protected IActionResult Success<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message));
    }

    protected IActionResult CreatedResult<T>(string uri, T data, string message = "Resource created successfully")
    {
        return Created(uri, ApiResponse<T>.SuccessResponse(data, message));
    }
}

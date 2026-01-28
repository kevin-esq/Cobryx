using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.RefreshToken;
using Concordia;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var updatedCommand = command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0" };
        var result = await _sender.Send(updatedCommand);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var updatedCommand = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
            DeviceFingerprint = Request.Headers["User-Agent"].ToString()
        };
        var result = await _sender.Send(updatedCommand);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(result.Error);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
    {
        var updatedCommand = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
            DeviceFingerprint = Request.Headers["User-Agent"].ToString()
        };
        var result = await _sender.Send(updatedCommand);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

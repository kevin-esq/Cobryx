using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.RefreshToken;
using Concordia;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : CobryxBaseController
{
    public AuthController(ISender sender) : base(sender)
    {
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Registration successful");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Login successful");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Token refreshed successfully");
    }
}

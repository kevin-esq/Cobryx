using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Commands.RefreshToken;
using Cobryx.Application.Auth.Commands.Sessions;
using Cobryx.Application.Auth.Common;
using Concordia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cobryx.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : CobryxBaseController
{
    public AuthController(ISender sender) : base(sender)
    {
    }

    [Cobryx.Api.Infrastructure.ValidateCaptcha]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Registration successful. Please verify your email.");
    }

    [Cobryx.Api.Infrastructure.ValidateCaptcha]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Login successful");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Token refreshed successfully");
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Email verified successfully");
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Verification email sent (if account exists)");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Password reset email sent (if account exists)");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Password reset successfully");
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Password changed successfully");
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LogoutCommand(), cancellationToken);
        return HandleResult(result, "Logged out successfully");
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LogoutAllCommand(), cancellationToken);
        return HandleResult(result, "All sessions revoked successfully");
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSessionsQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("sessions/{id}")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeSessionCommand(id), cancellationToken);
        return HandleResult(result, "Session revoked successfully");
    }

    [HttpPost("external-login")]
    public async Task<IActionResult> ExternalLogin(ExternalLoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Login successful");
    }
}

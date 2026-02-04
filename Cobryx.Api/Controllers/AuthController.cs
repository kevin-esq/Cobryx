using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Sessions;
using Cobryx.Application.Auth.Commands.RefreshToken;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Tenants.Commands.OnboardBusiness;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Concordia;
using Cobryx.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Provides authentication and session management services.
/// </summary>
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : CobryxBaseController
{
    private readonly ICookieService _cookieService;

    public AuthController(ISender sender, ICookieService cookieService) : base(sender)
    {
        _cookieService = cookieService;
    }

    /// <summary>
    /// Lightweight user and tenant registration.
    /// Focuses strictly on identity and basic business name.
    /// </summary>
    [HttpPost("signup")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> SignUp(SignUpCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedResult("/api/auth/login", result, "Account created successfully. Please verify your email.");
    }

    /// <summary>
    /// Completes business onboarding with fiscal and industry details.
    /// Requires an authenticated session.
    /// </summary>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("onboard")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Onboard(OnboardBusinessCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "Business onboarding completed successfully.");
    }

    /// <summary>
    /// Verifies a user's email address using a security token.
    /// </summary>
    /// <param name="command">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Returns when the email is successfully verified.</response>
    /// <response code="400">Returns when the token is invalid or expired.</response>
    [HttpPost("verify-email")]
    [SkipOnboardingCheck]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        if (result.IsSuccess) return NoContent();

        return HandleResult(result);
    }

    /// <summary>
    /// Resends the email verification link if the user exists and the email has not yet been verified.
    /// The response is always successful to prevent account enumeration.
    /// </summary>
    [HttpPost("resend-verification")]
    [ValidateCaptcha]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 429)]
    public async Task<IActionResult> ResendVerification(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, "If an account exists with this email, a verification link has been sent.");
    }

    /// <summary>
    /// Authenticates a user and returns an access token.
    /// A secure HttpOnly refresh token cookie is also set.
    /// </summary>
    /// <param name="command">The login credentials and device information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result including the JWT access token.</returns>
    /// <response code="200">Returns the access token and sets the refresh token cookie.</response>
    /// <response code="400">Returns when credentials are invalid or the account is locked.</response>
    /// <response code="403">Returns when the email is not verified (includes VERIFY_EMAIL action).</response>
    [HttpPost("login")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResult>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        if (result.IsSuccess && result.Value?.RefreshToken != null && result.Value.RefreshExpires.HasValue)
        {
            _cookieService.SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshExpires.Value);
        }

        return HandleResult(result, "Login successful");
    }

    /// <summary>
    /// Refreshes the authentication session using the refresh token stored in the secure cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new JWT access token and a rotated refresh token cookie.</returns>
    /// <response code="200">Returns a new access token and rotates the refresh token cookie.</response>
    /// <response code="401">Returns when the refresh token is missing, invalid, or expired (success: false).</response>
    [HttpPost("refresh-token")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResult>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RefreshTokenCommand(), cancellationToken);
        if (result.IsSuccess && result.Value?.RefreshToken != null && result.Value.RefreshExpires.HasValue)
        {
            _cookieService.SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshExpires.Value);
        }

        return HandleResult(result, "Token refreshed successfully");
    }

    /// <summary>
    /// Logs out the current session and clears the refresh token cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Successfully logged out and cookie cleared.</response>
    /// <response code="401">Unauthorized (success: false).</response>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("logout")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await Sender.Send(new LogoutCommand(), cancellationToken);
        _cookieService.DeleteRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>
    /// Logs out all active sessions for the user and clears the refresh token cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Successfully logged out of all devices and cookie cleared.</response>
    /// <response code="401">Unauthorized (success: false).</response>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("logout-all")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await Sender.Send(new LogoutAllCommand(), cancellationToken);
        _cookieService.DeleteRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>
    /// Retrieves a list of all active login sessions for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active sessions with device and IP information.</returns>
    /// <response code="200">Returns the list of active sessions.</response>
    /// <response code="401">Unauthorized (success: false).</response>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<SessionResponse>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSessionsQuery(), cancellationToken);
        return HandleResult(result);
    }
}

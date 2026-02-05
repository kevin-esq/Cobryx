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
using Cobryx.Api.Outcomes;
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
    /// <summary>
    /// Registers a new user and tenant (Sign Up).
    /// </summary>
    /// <param name="command">The registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success response with the status of the registration (e.g., verification required).</returns>
    /// <response code="201">User successfully registered.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">User already exists.</response>
    [HttpPost("signup")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> SignUp(SignUpCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleCreatedResult("/api/auth/login", result, AuthOutcomes.SignupVerificationRequired);
    }

    /// <summary>
    /// Onboards a business tenant with additional details.
    /// </summary>
    /// <param name="command">The onboarding details (Tax ID, Sector, Address).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="200">Onboarding completed successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="409">Tenant already onboarded.</response>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("onboard")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Onboard(OnboardBusinessCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Verifies a user's email address using a token.
    /// </summary>
    /// <param name="command">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="200">Email successfully verified.</response>
    /// <response code="400">Validation failed (e.g. empty token).</response>
    /// <response code="401">Invalid or expired token.</response>
    [HttpPost("verify-email")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, AuthOutcomes.EmailVerified);
    }

    /// <summary>
    /// Resends the email verification link.
    /// </summary>
    /// <param name="command">The email address to resend to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status (always returns success for security reasons unless rate limited).</returns>
    /// <response code="200">Verification email sent (or simulated).</response>
    /// <response code="400">Validation failed (e.g. invalid email format) or Captcha failed.</response>
    /// <response code="429">Too many requests.</response>
    [HttpPost("resend-verification")]
    [ValidateCaptcha]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 429)]
    public async Task<IActionResult> ResendVerification(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, AuthOutcomes.VerificationEmailSent);
    }

    /// <summary>
    /// Authenticates a user and issues a JWT token.
    /// </summary>
    /// <param name="command">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth result containing the access token and user info.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Validation failed (e.g. missing fields).</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="403">Account locked or email not verified.</response>
    [HttpPost("login")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResult>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        if (result.IsSuccess && result.Value?.RefreshToken != null && result.Value.RefreshExpires.HasValue)
        {
            _cookieService.SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshExpires.Value);
        }

        string? code = result.IsSuccess && result.Value?.Token == null ? AuthOutcomes.LoginMfaRequired : AuthOutcomes.LoginCompleted;
        return HandleResult(result, code);
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

        return HandleResult(result);
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

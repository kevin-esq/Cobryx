using Asp.Versioning;

using Cobryx.Api.Filters;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Commands.Enroll;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.RefreshToken;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.Sessions;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Tenants.Commands.OnboardBusiness;
using Cobryx.Application.Tenants.Common;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Provides authentication and session management services.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Platform")]
public class AuthController(ISender sender, ICookieService cookieService) : CobryxBaseController(sender)
{
    /// <summary>
    /// Registers a new user and tenant (Sign Up).
    /// </summary>
    /// <remarks>
    /// Creates a new tenant and an owner user. An email verification link is sent automatically.
    /// Possible Outcomes:
    /// - AUTH.SIGNUP.VERIFICATION_REQUIRED: Signup successful, email verification sent using ReturnUrl or AppUrl fallback.
    /// - AUTH.SIGNUP.FAILED: Invalid data or email already exists.
    /// </remarks>
    /// <param name="request">The registration details including business name and optional return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("signup")]
    [Idempotent]
    [AllowAnonymous]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request, CancellationToken cancellationToken)
    {
        var command = new SignUpCommand(
            request.BusinessName,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            ReturnUrl: request.ReturnUrl);

        Result<Guid> result = await Sender.Send(command, cancellationToken);
        return HandleCreatedResult("/api/v1/auth/login", result, AuthOutcomes.SignupVerificationRequired);
    }

    /// <summary>
    /// Enrolls an invited user into their tenant.
    /// </summary>
    /// <remarks>
    /// Consumes an invitation token and creates a user account.
    /// Possible Outcomes:
    /// - TENANT.INVITATION.ENROLL_SUCCESS: User enrolled and email automatically verified.
    /// - AUTH.ENROLL.FAILED: Invalid token or email mismatch.
    /// </remarks>
    /// <param name="request">Enrollment details including the secure invitation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("enroll")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 402)]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken cancellationToken)
    {
        var command = new EnrollUserCommand(
            request.Token,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            request.MarketingConsent);

        Result<Guid> result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, InvitationOutcomes.EnrollSuccess);
    }

    /// <summary>
    /// Onboards a business tenant with additional details.
    /// </summary>
    /// <remarks>
    /// Completes the tenant profile with tax information and industry data. Required for core financial features.
    /// Possible Outcomes:
    /// - TENANT.ONBOARDING.COMPLETED: Business details successfully registered.
    /// - TENANT.ONBOARDING.FAILED: Invalid TaxID or industry data.
    /// </remarks>
    /// <param name="request">The onboarding details (Tax ID, Sector, Address).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("onboard")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Onboard([FromBody] OnboardRequest request, CancellationToken cancellationToken)
    {
        var command = new OnboardBusinessCommand(
            request.TaxId,
            request.Industry,
            request.BusinessAddress.ToFormattedString(),
            request.Phone);

        Result result = await Sender.Send(command, cancellationToken);
        return HandleResult(result, TenantOutcomes.OnboardingCompleted);
    }

    /// <summary>
    /// Verifies a user's email address using a token.
    /// </summary>
    /// <remarks>
    /// Consumes the verification token and marks the user as verified.
    ///
    /// Possible Outcomes:
    /// - AUTH.EMAIL_VERIFIED: Email verified successfully.
    /// - AUTH.VERIFICATION.FAILED: Invalid or expired token.
    /// </remarks>
    /// <param name="request">The verification token received via email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("email/verify")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await Sender.Send(new VerifyEmailCommand(request.Token), cancellationToken);
        return HandleResult(result, AuthOutcomes.EmailVerified);
    }

    /// <summary>
    /// Resends the email verification link.
    /// </summary>
    /// <remarks>
    /// Generates a new verification link and sends it to the user's email. Rate limited to prevant abuse.
    ///
    /// Possible Outcomes:
    /// - AUTH.VERIFICATION_EMAIL_SENT: New verification link sent using ReturnUrl or AppUrl fallback.
    /// - AUTH.VERIFICATION.FAILED: Email not found or rate limited.
    /// </remarks>
    /// <param name="request">The email to verify and optional return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("email/resend")]
    [ValidateCaptcha]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 429)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        Result result =
            await Sender.Send(new ResendVerificationCommand(request.Email, request.CaptchaToken, request.ReturnUrl),
                cancellationToken);
        return HandleResult(result, AuthOutcomes.VerificationEmailSent);
    }

    /// <summary>
    /// Authenticates a user and issues a JWT token.
    /// </summary>
    /// <remarks>
    /// Validates credentials and returns a JWT token. If MFA is enabled, a partial success outcome is returned with an MFA token.
    ///
    /// Possible Outcomes:
    /// - AUTH.LOGIN.COMPLETED: Authentication successful, token issued.
    /// - AUTH.LOGIN.MFA_REQUIRED: Credentials valid, but MFA step is needed.
    /// - AUTH.LOGIN.FAILED: Invalid credentials or account issues.
    /// </remarks>
    /// <param name="request">Login credentials including captcha token if required.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResponseContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password, CaptchaToken: request.CaptchaToken);
        Result<AuthResult> result = await Sender.Send(command, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, AuthOutcomes.LoginCompleted);
        }

        if (result.Value.RefreshToken != null && result.Value.RefreshExpires != null)
        {
            cookieService.SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshExpires.Value);
        }

        var mappedResult = new AuthResponseContract(
            result.Value.Token,
            result.Value.FirstName,
            result.Value.LastName,
            result.Value.FullName,
            result.Value.Email,
            result.Value.Role,
            result.Value.Expires,
            result.Value.SessionId,
            result.Value.RequiresMfa,
            result.Value.MfaToken,
            result.Value.RequiresOnboarding);

        Outcome code = result.Value.Token == null
            ? AuthOutcomes.LoginMfaRequired
            : AuthOutcomes.LoginCompleted;

        return Success(mappedResult, code);
    }

    /// <summary>
    /// Refreshes the authentication session using the refresh token stored in the secure cookie.
    /// </summary>
    /// <remarks>
    /// Validates the refresh token from the 'refreshToken' cookie and issues a new access token and a rotated refresh token.
    ///
    /// Possible Outcomes:
    /// - AUTH.TOKEN.ROTATED: Session extended with new tokens.
    /// - AUTH.TOKEN.FAILED: Invalid or expired refresh token.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new JWT access token and a rotated refresh token cookie.</returns>
    /// <response code="200">Returns a new access token and rotates the refresh token cookie.</response>
    /// <response code="401">Returns when the refresh token is missing, invalid, or expired (success: false).</response>
    [HttpPost("token/refresh")]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResponseContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        Result<AuthResult> result = await Sender.Send(new RefreshTokenCommand(), cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, AuthOutcomes.TokenRotated);
        }

        if (result.Value.RefreshToken != null && result.Value.RefreshExpires != null)
        {
            cookieService.SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshExpires.Value);
        }

        var mappedResult = new AuthResponseContract(
            result.Value.Token,
            result.Value.FirstName,
            result.Value.LastName,
            result.Value.FullName,
            result.Value.Email,
            result.Value.Role,
            result.Value.Expires,
            result.Value.SessionId,
            result.Value.RequiresMfa,
            result.Value.MfaToken,
            result.Value.RequiresOnboarding);

        return Success(mappedResult, AuthOutcomes.TokenRotated);
    }

    /// <summary>
    /// Logs out the current session and clears the refresh token cookie.
    /// </summary>
    /// <remarks>
    /// Revokes the current session and deletes the 'refreshToken' cookie.
    ///
    /// Possible Outcomes:
    /// - AUTH.LOGOUT.COMPLETED: Logout successful.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await Sender.Send(new LogoutCommand(), cancellationToken);
        cookieService.DeleteRefreshTokenCookie();
        return Ok(ApiResponseFactory.Success(outcomeCode: AuthOutcomes.LogoutCompleted));
    }

    /// <summary>
    /// Logs out all active sessions for the user and clears the refresh token cookie.
    /// </summary>
    /// <remarks>
    /// Revokes all active sessions for the user across all devices.
    ///
    /// Possible Outcomes:
    /// - AUTH.LOGOUT_ALL.COMPLETED: All sessions successfully revoked.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [Authorize]
    [SkipOnboardingCheck]
    [HttpPost("sessions/revoke-all")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await Sender.Send(new LogoutAllCommand(), cancellationToken);
        cookieService.DeleteRefreshTokenCookie();
        return Ok(ApiResponseFactory.Success(outcomeCode: AuthOutcomes.LogoutAllCompleted));
    }

    /// <summary>
    /// Initiates the password reset flow by sending an email.
    /// </summary>
    /// <remarks>
    /// Generates a password reset token and sends an email to the user with a link using ReturnUrl or AppUrl fallback.
    ///
    /// Possible Outcomes:
    /// - AUTH.PASSWORD_RESET_EMAIL_SENT: Email sent if user was found. Always returns success for security (preventing account enumeration).
    /// </remarks>
    /// <param name="request">The email to reset and optional return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [SkipOnboardingCheck]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result result =
            await Sender.Send(new ForgotPasswordCommand(request.Email, request.ReturnUrl), cancellationToken);
        return HandleResult(result, AuthOutcomes.PasswordResetEmailSent);
    }
}

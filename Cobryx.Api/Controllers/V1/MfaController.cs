using System.Text.Json;

using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Auth.Commands.Mfa;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Fido2NetLib;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for multifactor authentication (TOTP, FIDO2).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mfa")]
[Tags("Platform")]
public class MfaController(ISender sender, ICookieService cookieService) : CobryxBaseController(sender)
{
    private IActionResult CompleteMfaLogin(Result<AuthResult> result, Outcome successOutcome)
    {
        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, successOutcome);
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

        return Success(mappedResult, successOutcome);
    }

    /// <summary>
    /// Generates a new TOTP setup secret and QR code URI.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - AUTH.LOGIN.MFA_REQUIRED: Setup successfully initiated.
    /// - AUTH.MFA.FAILED: User already has MFA enabled or internal error.
    /// </remarks>
    [Authorize]
    [HttpGet("totp/setup")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetTotpSetup()
    {
        Result<TotpSetupResult> result = await Sender.Send(new GetTotpSetupQuery());
        return HandleResult(result, AuthOutcomes.LoginMfaRequired);
    }

    /// <summary>
    /// Activates TOTP for the current user using the provided code.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - AUTH.MFA.ENABLED: TOTP successfully activated.
    /// - AUTH.MFA.FAILED: Invalid verification code or already enabled.
    /// </remarks>
    [Authorize]
    [HttpPost("totp/activate")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> ActivateTotp([FromBody] EnableMfaRequest request)
    {
        var command = new EnableMfaCommand(request.Code, request.Secret);
        Result<List<string>> result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaEnabled);
    }

    /// <summary>
    /// Verifies a TOTP code during the login flow.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - AUTH.MFA.VERIFIED: Code valid, login completed.
    /// - AUTH.MFA.FAILED: Invalid code or expired persistence token.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("totp/verify")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResponseContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> VerifyTotp([FromBody] VerifyMfaRequest request)
    {
        var command = new VerifyTotpLoginCommand(request.PersistenceToken, request.Code);
        Result<AuthResult> result = await Sender.Send(command);
        return CompleteMfaLogin(result, AuthOutcomes.MfaVerified);
    }

    /// <summary>
    /// Initiates FIDO2 (WebAuthn) security key registration by providing a server challenge.
    /// </summary>
    /// <remarks>
    /// Browser/Mobile: The returned 'CredentialCreateOptions' must be passed to 'navigator.credentials.create()'.
    /// Ensure all binary fields (Id, Challenge) are correctly handled as ArrayBuffers.
    ///
    /// Possible Outcomes:
    /// - AUTH.LOGIN.MFA_REQUIRED: Challenge generated for the device.
    /// - AUTH.MFA.FAILED: Internal configuration error.
    /// </remarks>
    [Authorize]
    [HttpGet("fido2/setup")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> InitiateFido2Registration()
    {
        Result<CredentialCreateOptions> result = await Sender.Send(new InitiateFido2RegistrationCommand());

        return HandleResult(result, AuthOutcomes.LoginMfaRequired);
    }

    /// <summary>
    /// Completes FIDO2 registration using the device registration data.
    /// </summary>
    /// <remarks>
    /// Payload: Expects a serialized 'AuthenticatorAttestationRawResponse'.
    /// Important: Binary blobs in the response must be base64url encoded for the JSON payload.
    ///
    /// Possible Outcomes:
    /// - AUTH.FIDO2.REGISTERED: Passkey successfully registered.
    /// - AUTH.MFA.FAILED: Invalid device registration data.
    /// </remarks>
    [Authorize]
    [HttpPost("fido2/activate")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> CompleteFido2Registration([FromBody] CompleteFido2RegistrationRequest request)
    {
        AuthenticatorAttestationRawResponse? response =
            request.RegistrationData.Response.Deserialize<AuthenticatorAttestationRawResponse>();
        CredentialCreateOptions? options = request.Challenge.Options.Deserialize<CredentialCreateOptions>();

        if (response == null || options == null)
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: AuthOutcomes.Fido2InvalidPayload,
                errors:
                [
                    new ValidationError("registrationData", ValidationCodes.InvalidPayload,
                        "Invalid passkey registration payload. One or more binary attributes are malformed.")
                ]));
        }

        var command = new CompleteFido2RegistrationCommand(request.DeviceName, response, options);
        Result<bool> result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.Fido2Registered);
    }

    /// <summary>
    /// Initiates FIDO2 verification (passkey challenge).
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - AUTH.MFA.INITIATED: Challenge generated for the user's passkeys.
    /// - AUTH.MFA.FAILED: User has no registered passkeys.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("fido2/challenge")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> InitiateFido2Assertion([FromBody] InitiateFido2AssertionRequest request)
    {
        var command = new InitiateFido2AssertionCommand(request.PersistenceToken);
        Result<AssertionOptions> result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaInitiated);
    }

    /// <summary>
    /// Completes FIDO2 verification after the device signature.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - AUTH.MFA.VERIFIED: Signature valid, login completed.
    /// - AUTH.MFA.FAILED: Invalid signature or challenge mismatch.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("fido2/verify")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResponseContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> CompleteFido2Assertion([FromBody] CompleteFido2AssertionRequest request)
    {
        AuthenticatorAssertionRawResponse? response =
            request.VerificationData.Response.Deserialize<AuthenticatorAssertionRawResponse>();
        AssertionOptions? optionsData = request.Challenge.Options.Deserialize<AssertionOptions>();

        if (response == null || optionsData == null)
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: AuthOutcomes.Fido2InvalidPayload,
                errors:
                [
                    new ValidationError("verificationData", ValidationCodes.InvalidPayload,
                        "Invalid passkey verification payload. Check binary encoding.")
                ]));
        }

        var command = new CompleteFido2AssertionCommand(request.PersistenceToken, response, optionsData);
        Result<AuthResult> result = await Sender.Send(command);
        return CompleteMfaLogin(result, AuthOutcomes.MfaVerified);
    }
}

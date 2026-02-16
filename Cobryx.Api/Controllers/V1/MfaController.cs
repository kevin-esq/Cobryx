using Asp.Versioning;
using Cobryx.Application.Auth.Commands.Mfa;
using Cobryx.Api.Contracts.V1.Identity;
using Cobryx.Api.Contracts.V1.Common;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;
using Fido2NetLib;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for multi-factor authentication (TOTP, FIDO2).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mfa")]
[Tags("Identity & Access")]
public class MfaController : CobryxBaseController
{
    public MfaController(ISender sender) : base(sender)
    {
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
    [HttpGet("setup-totp")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetTotpSetup()
    {
        var result = await Sender.Send(new GetTotpSetupQuery());
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
    [HttpPost("activate-totp")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> ActivateTotp([FromBody] EnableMfaRequest request)
    {
        // Intentional Mapping: Public Intent -> Internal Implementation
        var command = new EnableMfaCommand(request.Code, request.Secret);
        var result = await Sender.Send(command);
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
    [HttpPost("verify-totp")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResponseContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> VerifyTotp([FromBody] VerifyMfaRequest request)
    {
        // Intentional Mapping: Public Intent -> Internal Implementation
        var command = new VerifyTotpLoginCommand(request.PersistenceToken, request.Code);
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaVerified);
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
    [HttpGet("setup-fido2")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> InitiateFido2Registration()
    {
        var result = await Sender.Send(new InitiateFido2RegistrationCommand());

        // Potential Improvement: Map result.Value (CredentialCreateOptions) to Fido2RegistrationOptions if needed
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
    [HttpPost("activate-fido2")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> CompleteFido2Registration([FromBody] CompleteFido2RegistrationRequest request)
    {
        // Intentional Mapping: Business Contract -> Internal Library Types
        var response = request.RegistrationData.Response.Deserialize<AuthenticatorAttestationRawResponse>();
        var options = request.Challenge.Options.Deserialize<CredentialCreateOptions>();

        if (response == null || options == null)
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: AuthOutcomes.Fido2InvalidPayload,
                errors: new[] { new Cobryx.Api.Contracts.V1.Common.ValidationError("registrationData", "INVALID_PAYLOAD", "Invalid passkey registration payload. One or more binary attributes are malformed.") }));
        }

        var command = new CompleteFido2RegistrationCommand(request.DeviceName, response, options);
        var result = await Sender.Send(command);
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
    [HttpPost("verify-fido2-init")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> InitiateFido2Assertion([FromBody] InitiateFido2AssertionRequest request)
    {
        // Intentional Mapping: Public Intent -> Internal Implementation
        var command = new InitiateFido2AssertionCommand(request.PersistenceToken);
        var result = await Sender.Send(command);
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
    [HttpPost("verify-fido2")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AuthResponseContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> CompleteFido2Assertion([FromBody] CompleteFido2AssertionRequest request)
    {
        // Intentional Mapping: Business Contract -> Internal Library Types
        var response = request.VerificationData.Response.Deserialize<AuthenticatorAssertionRawResponse>();
        var optionsData = request.Challenge.Options.Deserialize<AssertionOptions>();

        if (response == null || optionsData == null)
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: AuthOutcomes.Fido2InvalidPayload,
                errors: new[] { new Cobryx.Api.Contracts.V1.Common.ValidationError("verificationData", "INVALID_PAYLOAD", "Invalid passkey verification payload. Check binary encoding.") }));
        }

        var command = new CompleteFido2AssertionCommand(request.PersistenceToken, response, optionsData);
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaVerified);
    }
}

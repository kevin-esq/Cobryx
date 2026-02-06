using Cobryx.Application.Auth.Commands.Mfa;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;

namespace Cobryx.Api.Controllers;

[ApiController]
[Route("api/mfa")]
public class MfaController : CobryxBaseController
{
    public MfaController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("setup-totp")]
    public async Task<IActionResult> GetTotpSetup()
    {
        var result = await Sender.Send(new GetTotpSetupQuery());
        return HandleResult(result, AuthOutcomes.LoginMfaRequired);
    }

    [Authorize]
    [HttpPost("activate-totp")]
    public async Task<IActionResult> ActivateTotp(EnableMfaCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaEnabled);
    }

    [AllowAnonymous]
    [HttpPost("verify-totp")]
    public async Task<IActionResult> VerifyTotp(VerifyTotpLoginCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaVerified);
    }

    [Authorize]
    [HttpGet("setup-fido2")]
    public async Task<IActionResult> InitiateFido2Registration()
    {
        var result = await Sender.Send(new InitiateFido2RegistrationCommand());
        return HandleResult(result, AuthOutcomes.LoginMfaRequired);
    }

    [Authorize]
    [HttpPost("activate-fido2")]
    public async Task<IActionResult> CompleteFido2Registration(CompleteFido2RegistrationCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.Fido2Registered);
    }

    [AllowAnonymous]
    [HttpPost("verify-fido2-init")]
    public async Task<IActionResult> InitiateFido2Assertion(InitiateFido2AssertionCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaInitiated);
    }

    [AllowAnonymous]
    [HttpPost("verify-fido2")]
    public async Task<IActionResult> CompleteFido2Assertion(CompleteFido2AssertionCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, AuthOutcomes.MfaVerified);
    }
}

using Asp.Versioning;

using Cobryx.Application.Payments.Commands.InitializePaymentLink;
using Cobryx.Application.Payments.Queries.GetPaymentLinkByToken;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Public portal for customers to view and pay their payment links anonymously.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/public/pay")]
[AllowAnonymous]
[Tags("Customer Portal")]
public class PublicPaymentController : CobryxBaseController
{
    public PublicPaymentController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Retrieves payment link details using a secure token.
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> GetDetails(string token)
    {
        var result = await Sender.Send(new GetPaymentLinkByTokenQuery(token));
        return HandleResult((Result<PaymentLinkDto>)result, Outcome.FromExternal("PORTAL.PAYMENT.READ"));
    }

    /// <summary>
    /// Initializes a Stripe PaymentIntent for the given link.
    /// </summary>
    [HttpPost("{token}/initialize")]
    public async Task<IActionResult> Initialize(string token)
    {
        var result = await Sender.Send(new InitializePaymentLinkCommand(token));
        return HandleResult((Result<string>)result, Outcome.FromExternal("PORTAL.PAYMENT.INITIALIZE"));
    }

    /// <summary>
    /// Polls the status of a payment link.
    /// </summary>
    [HttpGet("{token}/status")]
    public async Task<IActionResult> GetStatus(string token)
    {
        var result = await Sender.Send(new GetPaymentLinkByTokenQuery(token));

        return HandleResult((Result<PaymentLinkDto>)result, Outcome.FromExternal("PORTAL.PAYMENT.STATUS"));
    }
}

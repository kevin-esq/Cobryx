using Asp.Versioning;
using Cobryx.Application.Payments.Commands.CreatePaymentLink;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Management API for Tenants to generate and control payment links.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment-links")]
[Authorize]
[Tags("Payments & Collection")]
public class PaymentLinksController : CobryxBaseController
{
    public PaymentLinksController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Generates a new secure payment link for a customer or specific loan.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentLinkCommand command)
    {
        var result = await Sender.Send(command);

        // Note: Success returns the RAW token which the tenant should 
        // immediately send to the customer (via email/SMS).
        return HandleResult(result, Outcome.FromExternal("PAYMENT_LINK.CREATE"));
    }
}

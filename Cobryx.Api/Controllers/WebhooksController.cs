using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Models;
using Cobryx.Application.Payments.Webhooks.Commands.ProcessWebhook;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : CobryxBaseController
{
    public WebhooksController(ISender sender) : base(sender)
    {
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeReceive()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: "API.WEBHOOK.EMPTY_BODY",
                errors: "Webhook request body is empty. State: " + Request.Body.CanRead));
        }

        if (!json.Trim().StartsWith("{"))
        {
            return BadRequest("Invalid JSON payload.");
        }

        var externalEventId = Guid.NewGuid().ToString();

        var command = new ProcessWebhookCommand("Stripe", externalEventId, json);
        var result = await Sender.Send(command);

        return HandleResult(result, InvoicingOutcomes.Webhooks.Received);
    }
}

using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for receiving and orchestrating external webhook events from third-party financial providers.
/// Supports high-priority processing for Stripe and other integrated services.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/webhooks")]
[Tags("System & Administration")]
public class WebhooksController : CobryxBaseController
{
    public WebhooksController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Processes incoming webhook events from Stripe integration.
    /// </summary>
    /// <remarks>
    /// Validates the JSON payload and queues the event for internal state reconciliation.
    ///
    /// Possible Outcomes:
    /// - WEBHOOK.RECEIVED: Event successfully accepted and processed.
    /// </remarks>
    /// <response code="200">Acknowledgement to Stripe that the event was received.</response>
    /// <response code="400">Malformed JSON or empty payload.</response>
    [HttpPost("stripe")]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 400)]
    public async Task<IActionResult> StripeReceive()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: "API.WEBHOOK.EMPTY_BODY",
                errors: new[] { new Cobryx.Api.Contracts.V1.Common.ValidationError("body", "EMPTY_BODY", "Webhook request body is empty.") }));
        }

        if (!json.Trim().StartsWith("{"))
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: "API.WEBHOOK.INVALID_JSON",
                errors: new[] { new Cobryx.Api.Contracts.V1.Common.ValidationError("body", "INVALID_JSON", "Invalid JSON payload.") }));
        }

        var externalEventId = Guid.NewGuid().ToString();

        var command = new Application.Payments.Webhooks.Commands.ProcessWebhook.ProcessWebhookCommand("Stripe", externalEventId, json);
        var result = await Sender.Send(command);

        return HandleResult(result, InvoicingOutcomes.Webhooks.Received);
    }
}

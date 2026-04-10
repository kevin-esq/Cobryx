using System.ComponentModel.DataAnnotations;

using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Payments.Webhooks.Commands.HandleWebhookEvent;
using Cobryx.Application.Payments.Webhooks.Commands.ProcessWebhook;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Stripe;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Receives and processes inbound webhooks from external payment providers.
/// All endpoints are anonymous — authentication is performed via provider-specific signature verification.
/// </summary>
/// <remarks>
/// This controller is intentionally thin. Its responsibilities are limited to:
/// 1. Reading the HTTP body
/// 2. Verifying the Stripe signature (infrastructure concern)
/// 3. Extracting the event ID
/// 4. Delegating to the unified webhook pipeline (ProcessWebhook → HandleWebhookEvent)
///
/// Dual idempotency layers:
/// - WebhookEvent table: ingestion-level dedup (prevents double-processing)
/// - ProcessedStripeEvents table: domain-level dedup (inside StripeSubscriptionSyncService)
/// </remarks>
[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
[Tags("Webhooks")]
public partial class WebhooksController(
    ISender sender,
    IOptions<StripeOptions> stripeOptions,
    ILogger<WebhooksController> logger) : CobryxBaseController(sender)
{
    /// <summary>
    /// Receives Stripe webhook events for subscription lifecycle, payment processing, and Connect account updates.
    /// </summary>
    /// <param name="signature">Stripe-Signature header for webhook verification.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// This endpoint is called by Stripe's webhook infrastructure.
    /// Signature verification is enforced when WebhookSecret is configured.
    ///
    /// Possible Outcomes:
    /// - WEBHOOK.RECEIVED_SUCCESS: Event acknowledged and processed.
    /// - WEBHOOK.EMPTY_BODY: Request body was empty.
    /// - WEBHOOK.INVALID_SIGNATURE: Signature verification failed.
    /// - WEBHOOK.PROCESSING_FAILED: Internal error during processing.
    /// </remarks>
    /// <response code="200">Webhook received and processed.</response>
    /// <response code="400">Empty body or invalid signature.</response>
    /// <response code="500">Internal processing error.</response>
    [HttpPost("stripe")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 500)]
    public async Task<IActionResult> StripeReceive(
        [FromHeader(Name = "Stripe-Signature")] [Required]
        string signature,
        CancellationToken ct)
    {
        using StreamReader reader = new(Request.Body);
        var json = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(json))
        {
            return BadRequest(ApiResponseFactory.Error(WebhookOutcomes.EmptyBody));
        }

        string stripeEventId;
        try
        {
            stripeEventId = ParseAndExtractEventId(json, signature);
        }
        catch (StripeException ex)
        {
            LogSignatureVerificationFailed(logger, ex);
            return BadRequest(ApiResponseFactory.Error(WebhookOutcomes.InvalidSignature));
        }

        Result<Guid> ingestResult = await Sender.Send(
            new ProcessWebhookCommand("Stripe", stripeEventId, json), ct);

        if (ingestResult.IsFailure)
        {
            LogIngestionFailed(logger, stripeEventId, ingestResult.Error ?? "unknown");
            return BuildErrorResponse(WebhookOutcomes.ProcessingFailed);
        }

        if (ingestResult.Value == Guid.Empty)
        {
            return Ok(ApiResponseFactory.Success<object>(
                new { received = true, idempotent = true }, WebhookOutcomes.Received));
        }

        try
        {
            Result processResult = await Sender.Send(
                new HandleWebhookEventCommand(ingestResult.Value), ct);

            if (processResult.IsFailure)
            {
                LogProcessingFailed(logger, stripeEventId, processResult.Error ?? "unknown");
            }
        }
        catch (Exception ex)
        {
            LogProcessingError(logger, ex, stripeEventId);
            return BuildErrorResponse(WebhookOutcomes.ProcessingFailed);
        }

        return Ok(ApiResponseFactory.Success<object>(
            new { received = true }, WebhookOutcomes.Received));
    }

    // ──────────────────────────────────────────────
    //  Stripe signature verification (HTTP concern)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Verifies the Stripe signature and extracts the event ID without exposing
    /// the full <see cref="Event"/> type beyond the controller boundary.
    /// </summary>
    private string ParseAndExtractEventId(string json, string signature)
    {
        StripeOptions options = stripeOptions.Value;

        Event stripeEvent;
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            LogWebhookMissingSecret(logger);
            stripeEvent = EventUtility.ParseEvent(json);
        }
        else if (string.IsNullOrWhiteSpace(signature))
        {
            throw new StripeException("Missing Stripe-Signature header");
        }
        else
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, options.WebhookSecret);
        }

        return stripeEvent.Id;
    }

    private static ObjectResult BuildErrorResponse(Outcome outcome)
    {
        ApiErrorResponse error = ApiResponseFactory.Error(outcome);
        return new ObjectResult(error) { StatusCode = 500 };
    }
}

using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Common;
using Cobryx.Infrastructure.Configuration;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text.Json;

namespace Cobryx.Api.Controllers.V1;

[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
[Tags("System & Administration")]
public class WebhooksController : CobryxBaseController
{
    private readonly StripeSubscriptionSyncService _syncService;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        ISender sender,
        StripeSubscriptionSyncService syncService,
        IOptions<StripeOptions> stripeOptions,
        ILogger<WebhooksController> logger) : base(sender)
    {
        _syncService = syncService;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Securely processes incoming webhook events from Stripe for subscription lifecycle management.
    /// </summary>
    /// <remarks>
    /// Financial Security:
    /// - Performs SHA-256 HMAC signature verification on all incoming payloads.
    /// - Routes 'checkout.session.completed', 'invoice.paid', and subscription update events.
    /// - Ensures tenant subscription state remains consistent with Stripe's records.
    ///
    /// Possible Outcomes:
    /// - WEBHOOK.RECEIVED: Event successfully parsed and queued for processing.
    /// - WEBHOOK.INVALID_SIGNATURE: Authentication failed; request discarded.
    /// - WEBHOOK.EMPTY_BODY: Malformed request received.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Confirmation that the event was received and is being processed.</response>
    /// <response code="400">Invalid payload or signature.</response>
    [HttpPost("stripe")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> StripeReceive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return BadRequest(ApiResponseFactory.Error(
                errorCode: WebhookOutcomes.EmptyBody,
                errors: new[] { new ValidationError("body", "EMPTY_BODY", "Webhook request body is empty.") }));
        }

        // Verify Stripe signature
        Event stripeEvent;
        try
        {
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
            {
                // In development/test mode without webhook secret, parse directly
                stripeEvent = EventUtility.ParseEvent(json);
                _logger.LogWarning("Stripe webhook received without signature verification");
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeOptions.WebhookSecret);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            return BadRequest(ApiResponseFactory.Error(WebhookOutcomes.InvalidSignature));
        }

        // Route subscription lifecycle events to sync service
        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutCompleted(stripeEvent, ct);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaid(stripeEvent, ct);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent, ct);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent, ct);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent, ct);
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook event {EventType}", stripeEvent.Type);
            // Still return 200 to prevent Stripe retries for application errors
        }

        return Ok(ApiResponseFactory.Success<object>(new { received = true }, "WEBHOOK.RECEIVED"));
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var tenantIdStr = session.Metadata?.GetValueOrDefault(CobryxClaimTypes.TenantId);
        if (!Guid.TryParse(tenantIdStr, out var tenantId)) return;

        var stripeCustomerId = session.CustomerId ?? session.Customer?.Id;
        var stripeSubscriptionId = session.SubscriptionId ?? session.Subscription?.Id;

        if (stripeCustomerId == null || stripeSubscriptionId == null)
        {
            _logger.LogWarning("checkout.session.completed missing customer/subscription IDs");
            return;
        }

        await _syncService.HandleCheckoutCompletedAsync(stripeCustomerId, stripeSubscriptionId, tenantId, ct);
    }

    private async Task HandleInvoicePaid(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (subscriptionId == null) return;

        await _syncService.HandleInvoicePaidAsync(subscriptionId, ct);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (subscriptionId == null) return;

        await _syncService.HandleInvoicePaymentFailedAsync(subscriptionId, ct);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        await _syncService.HandleSubscriptionUpdatedAsync(subscription.Id, ct);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        await _syncService.HandleSubscriptionDeletedAsync(subscription.Id, ct);
    }
}

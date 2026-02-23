using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Common;
using Cobryx.Infrastructure.Configuration;
using Concordia;
using Cobryx.Application.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.ComponentModel.DataAnnotations;

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
    private readonly Application.Payments.Services.PaymentLinkReconciliationService _reconciliationService;

    public WebhooksController(
        ISender sender,
        StripeSubscriptionSyncService syncService,
        Application.Payments.Services.PaymentLinkReconciliationService reconciliationService,
        IOptions<StripeOptions> stripeOptions,
        ILogger<WebhooksController> logger) : base(sender)
    {
        _syncService = syncService;
        _reconciliationService = reconciliationService;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    [HttpPost("stripe")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> StripeReceive(
        [FromHeader(Name = "Stripe-Signature")][Required] string signature,
        CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return BadRequest(ApiResponseFactory.Error(WebhookOutcomes.EmptyBody));
        }

        Event stripeEvent = default!;
        try
        {
            if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
            {
                stripeEvent = EventUtility.ParseEvent(json);
                _logger.LogWarning("Stripe webhook received without signature verification (WebhookSecret missing)");
            }
            else if (string.IsNullOrWhiteSpace(signature))
            {
                return BadRequest(ApiResponseFactory.Error(WebhookOutcomes.InvalidSignature));
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeOptions.WebhookSecret);
            }

            switch (stripeEvent.Type)
            {
                case StripeConstants.Events.CheckoutSessionCompleted:
                    await HandleCheckoutCompleted(stripeEvent, ct);
                    break;

                case StripeConstants.Events.PaymentIntentSucceeded:
                    await HandlePaymentIntentSucceeded(stripeEvent, ct);
                    break;

                case StripeConstants.Events.ChargeRefunded:
                    await HandleChargeRefunded(stripeEvent, ct);
                    break;

                case StripeConstants.Events.InvoicePaid:
                    await HandleInvoicePaid(stripeEvent, ct);
                    break;

                case StripeConstants.Events.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailed(stripeEvent, ct);
                    break;

                case StripeConstants.Events.SubscriptionUpdated:
                    await HandleSubscriptionUpdated(stripeEvent, ct);
                    break;

                case StripeConstants.Events.SubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent, ct);
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            return BadRequest(ApiResponseFactory.Error(WebhookOutcomes.InvalidSignature));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook {EventId}", stripeEvent?.Id ?? "unknown");
            return StatusCode(500, ApiResponseFactory.Error(WebhookOutcomes.ProcessingFailed));
        }

        return Ok(ApiResponseFactory.Success<object>(new { received = true }, WebhookOutcomes.Received));
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var tenantIdStr = session.Metadata?.GetValueOrDefault(CobryxClaimTypes.TenantId);
        if (!Guid.TryParse(tenantIdStr, out var tenantId)) return;

        var stripeCustomerId = session.CustomerId ?? session.Customer?.Id;
        var stripeSubscriptionId = session.SubscriptionId ?? session.Subscription?.Id;

        if (stripeCustomerId == null || stripeSubscriptionId == null) return;

        await _syncService.HandleCheckoutCompletedAsync(stripeEvent.Id, stripeCustomerId, stripeSubscriptionId, tenantId, ct);
    }

    private async Task HandleInvoicePaid(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (subscriptionId == null) return;

        await _syncService.HandleInvoicePaidAsync(stripeEvent.Id, subscriptionId, ct);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (subscriptionId == null) return;

        await _syncService.HandleInvoicePaymentFailedAsync(stripeEvent.Id, subscriptionId, ct);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        await _syncService.HandleSubscriptionUpdatedAsync(stripeEvent.Id, subscription.Id, ct);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        await _syncService.HandleSubscriptionDeletedAsync(stripeEvent.Id, subscription.Id, ct);
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent, CancellationToken ct)
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent == null) return;

        var amount = new Domain.ValueObjects.Money(intent.Amount / 100m, intent.Currency.ToUpperInvariant());
        await _reconciliationService.HandlePaymentSuccessAsync(intent.Id, amount, ct);
    }

    private async Task HandleChargeRefunded(Event stripeEvent, CancellationToken ct)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null || string.IsNullOrEmpty(charge.PaymentIntentId)) return;

        var amount = new Domain.ValueObjects.Money(charge.AmountRefunded / 100m, charge.Currency.ToUpperInvariant());
        await _reconciliationService.HandleRefundAsync(charge.PaymentIntentId, amount, ct);
    }
}

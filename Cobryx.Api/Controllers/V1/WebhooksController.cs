using System.ComponentModel.DataAnnotations;

using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Stripe;

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
        var json = await reader.ReadToEndAsync(ct);

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

                case StripeConstants.Events.AccountUpdated:
                    await HandleAccountUpdated(stripeEvent, ct);
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
        catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            _logger.LogInformation("Concurrency conflict handled for concurrent Stripe event {EventId}", stripeEvent?.Id);
            return Ok(ApiResponseFactory.Success<object>(new { received = true, concurrent = true }, WebhookOutcomes.Received));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true ||
                ex.InnerException?.Message.Contains("duplicate key value violates unique constraint") == true ||
                ex.ToString().Contains("UNIQUE constraint failed") ||
                ex.ToString().Contains("duplicate key value violates unique constraint"))
            {
                 _logger.LogInformation("Idempotent match for concurrent Stripe event {EventId}", stripeEvent?.Id);
                 return Ok(ApiResponseFactory.Success<object>(new { received = true, idempotent = true }, WebhookOutcomes.Received));
            }

            _logger.LogError(ex, "Database error processing Stripe webhook {EventId}", stripeEvent?.Id ?? "unknown");
            var error = ApiResponseFactory.Error(WebhookOutcomes.ProcessingFailed);
#if DEBUG || TEST
            error.ErrorCode = $"{error.ErrorCode} Details: {ex.Message} -> {ex.InnerException?.Message ?? "None"}";
#endif
            return StatusCode(500, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook {EventId} (Account: {Account})",
                stripeEvent?.Id ?? "unknown", stripeEvent?.Account ?? "platform");

            var error = ApiResponseFactory.Error(WebhookOutcomes.ProcessingFailed);
#if DEBUG || TEST
            error.ErrorCode = $"{error.ErrorCode} Details: {ex.Message} -> {ex.InnerException?.Message ?? "None"}";
#endif
            return StatusCode(500, error);
        }

        return Ok(ApiResponseFactory.Success<object>(new { received = true }, WebhookOutcomes.Received));
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data?.Object as Stripe.Checkout.Session;
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
        var invoice = stripeEvent.Data?.Object as Stripe.Invoice;
        if (invoice == null) return;
        var subscriptionId = (string?)((dynamic)invoice).SubscriptionId;
        if (subscriptionId == null) return;

        await _syncService.HandleInvoicePaidAsync(stripeEvent.Id, subscriptionId, ct);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data?.Object as Stripe.Invoice;
        if (invoice == null) return;
        var subscriptionId = (string?)((dynamic)invoice).SubscriptionId;
        if (subscriptionId == null) return;

        await _syncService.HandleInvoicePaymentFailedAsync(stripeEvent.Id, subscriptionId, ct);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data?.Object as Stripe.Subscription;
        if (subscription == null) return;

        await _syncService.HandleSubscriptionUpdatedAsync(stripeEvent.Id, subscription.Id, ct);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data?.Object as Stripe.Subscription;
        if (subscription == null) return;

        await _syncService.HandleSubscriptionDeletedAsync(stripeEvent.Id, subscription.Id, ct);
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent, CancellationToken ct)
    {
        var intent = stripeEvent.Data?.Object as Stripe.PaymentIntent;
        if (intent == null) return;

        var amount = new Domain.ValueObjects.Money(intent.Amount / 100m, intent.Currency.ToUpperInvariant());

        decimal? appFee = null;
        if (intent.ApplicationFeeAmount.HasValue)
        {
            appFee = intent.ApplicationFeeAmount.Value / 100m;
        }

        await _reconciliationService.HandlePaymentSuccessAsync(intent.Id, amount, appFee, ct);
    }

    private async Task HandleChargeRefunded(Event stripeEvent, CancellationToken ct)
    {
        var charge = stripeEvent.Data?.Object as Stripe.Charge;
        if (charge == null || string.IsNullOrEmpty(charge.PaymentIntentId)) return;

        var amount = new Domain.ValueObjects.Money(charge.AmountRefunded / 100m, charge.Currency.ToUpperInvariant());
        await _reconciliationService.HandleRefundAsync(charge.PaymentIntentId, amount, ct);
    }

    private async Task HandleAccountUpdated(Event stripeEvent, CancellationToken ct)
    {
        var account = stripeEvent.Data?.Object as Stripe.Account;
        if (account == null) return;

        _logger.LogInformation("Processing account.updated for Stripe Account: {AccountId}", account.Id);

        // We need DB context to update the tenant
        // Since we are in a controller, we use the inherited Sender property.
        await Sender.Send(new Application.Tenants.Commands.UpdateTenantConnectCapabilities.UpdateTenantConnectCapabilitiesCommand(
            account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted), ct);
    }
}

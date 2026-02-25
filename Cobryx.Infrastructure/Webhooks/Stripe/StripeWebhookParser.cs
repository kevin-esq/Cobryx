using Cobryx.Application.Payments.Webhooks.Common;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Cobryx.Infrastructure.Webhooks.Stripe;

public class StripeWebhookParser : IWebhookParser
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeWebhookParser> _logger;

    public StripeWebhookParser(IOptions<StripeOptions> options, ILogger<StripeWebhookParser> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Provider => "Stripe";

    public Task<WebhookParseResult> ParseAsync(string json, string? signature = null)
    {
        Event stripeEvent;

        try
        {
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                stripeEvent = EventUtility.ParseEvent(json);
                _logger.LogWarning("Stripe webhook parsed without signature verification");
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _options.WebhookSecret);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            throw;
        }

        var result = stripeEvent.Type switch
        {
            StripeConstants.Events.PaymentIntentSucceeded => HandlePaymentIntentSucceeded(stripeEvent),
            StripeConstants.Events.PaymentIntentFailed => HandlePaymentIntentFailed(stripeEvent),
            StripeConstants.Events.CheckoutSessionCompleted => HandleCheckoutCompleted(stripeEvent),
            StripeConstants.Events.InvoicePaid => HandleInvoicePaid(stripeEvent),
            StripeConstants.Events.InvoicePaymentFailed => HandleInvoicePaymentFailed(stripeEvent),
            StripeConstants.Events.SubscriptionUpdated => HandleSubscriptionUpdated(stripeEvent),
            StripeConstants.Events.SubscriptionDeleted => HandleSubscriptionDeleted(stripeEvent),
            StripeConstants.Events.PayoutPaid => HandlePayoutPaid(stripeEvent),
            StripeConstants.Events.PayoutFailed => HandlePayoutFailed(stripeEvent),
            _ => throw new NotSupportedException($"Stripe event type {stripeEvent.Type} is not supported.")
        };

        return Task.FromResult(result);
    }

    private WebhookParseResult HandlePayoutPaid(Event e)
    {
        var payout = e.Data.Object as Payout;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PayoutPaid, payout!, payout!.Id);
    }

    private WebhookParseResult HandlePayoutFailed(Event e)
    {
        var payout = e.Data.Object as Payout;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PayoutFailed, payout!, payout!.Id);
    }

    private WebhookParseResult HandlePaymentIntentSucceeded(Event e)
    {
        var intent = e.Data.Object as PaymentIntent;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PaymentSucceeded, intent!, intent!.Id);
    }

    private WebhookParseResult HandlePaymentIntentFailed(Event e)
    {
        var intent = e.Data.Object as PaymentIntent;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PaymentFailed, intent!, intent!.Id);
    }

    private WebhookParseResult HandleCheckoutCompleted(Event e)
    {
        var session = e.Data.Object as global::Stripe.Checkout.Session;
        return new WebhookParseResult(WebhookConstants.InternalEvents.CheckoutCompleted, session!, session!.Id);
    }

    private WebhookParseResult HandleInvoicePaid(Event e)
    {
        var invoice = e.Data.Object as Invoice;
        return new WebhookParseResult(WebhookConstants.InternalEvents.InvoicePaid, invoice!, e.Id);
    }

    private WebhookParseResult HandleInvoicePaymentFailed(Event e)
    {
        var invoice = e.Data.Object as Invoice;
        return new WebhookParseResult(WebhookConstants.InternalEvents.InvoicePaymentFailed, invoice!, e.Id);
    }

    private WebhookParseResult HandleSubscriptionUpdated(Event e)
    {
        var subscription = e.Data.Object as Subscription;
        return new WebhookParseResult(WebhookConstants.InternalEvents.SubscriptionUpdated, subscription!, e.Id);
    }

    private WebhookParseResult HandleSubscriptionDeleted(Event e)
    {
        var subscription = e.Data.Object as Subscription;
        return new WebhookParseResult(WebhookConstants.InternalEvents.SubscriptionDeleted, subscription!, e.Id);
    }
}

using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Payments.Webhooks.Common;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Application.Subscriptions.Common;

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

        var metadata = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(stripeEvent.Account))
        {
            metadata["stripe_account_id"] = stripeEvent.Account;
        }

        var result = stripeEvent.Type switch
        {
            StripeConstants.Events.PaymentIntentSucceeded => HandlePaymentIntentSucceeded(stripeEvent, metadata),
            StripeConstants.Events.PaymentIntentFailed => HandlePaymentIntentFailed(stripeEvent, metadata),
            StripeConstants.Events.CheckoutSessionCompleted => HandleCheckoutCompleted(stripeEvent, metadata),
            StripeConstants.Events.InvoicePaid => HandleInvoicePaid(stripeEvent, metadata),
            StripeConstants.Events.InvoicePaymentFailed => HandleInvoicePaymentFailed(stripeEvent, metadata),
            StripeConstants.Events.SubscriptionUpdated => HandleSubscriptionUpdated(stripeEvent, metadata),
            StripeConstants.Events.SubscriptionDeleted => HandleSubscriptionDeleted(stripeEvent, metadata),
            StripeConstants.Events.PayoutPaid => HandlePayoutPaid(stripeEvent, metadata),
            StripeConstants.Events.PayoutFailed => HandlePayoutFailed(stripeEvent, metadata),
            _ => throw new NotSupportedException($"Stripe event type {stripeEvent.Type} is not supported.")
        };

        return Task.FromResult(result);
    }

    private static WebhookParseResult HandlePayoutPaid(Event e, Dictionary<string, string> metadata)
    {
        var payout = e.Data.Object as Payout;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PayoutPaid, payout!, payout!.Id, metadata);
    }

    private static WebhookParseResult HandlePayoutFailed(Event e, Dictionary<string, string> metadata)
    {
        var payout = e.Data.Object as Payout;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PayoutFailed, payout!, payout!.Id, metadata);
    }

    private static WebhookParseResult HandlePaymentIntentSucceeded(Event e, Dictionary<string, string> metadata)
    {
        var intent = e.Data.Object as PaymentIntent;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PaymentSucceeded, intent!, intent!.Id, metadata);
    }

    private static WebhookParseResult HandlePaymentIntentFailed(Event e, Dictionary<string, string> metadata)
    {
        var intent = e.Data.Object as PaymentIntent;
        return new WebhookParseResult(WebhookConstants.InternalEvents.PaymentFailed, intent!, intent!.Id, metadata);
    }

    private static WebhookParseResult HandleCheckoutCompleted(Event e, Dictionary<string, string> metadata)
    {
        var session = e.Data.Object as global::Stripe.Checkout.Session;
        return new WebhookParseResult(WebhookConstants.InternalEvents.CheckoutCompleted, session!, session!.Id, metadata);
    }

    private static WebhookParseResult HandleInvoicePaid(Event e, Dictionary<string, string> metadata)
    {
        var invoice = e.Data.Object as Invoice;
        return new WebhookParseResult(WebhookConstants.InternalEvents.InvoicePaid, invoice!, e.Id, metadata);
    }

    private static WebhookParseResult HandleInvoicePaymentFailed(Event e, Dictionary<string, string> metadata)
    {
        var invoice = e.Data.Object as Invoice;
        return new WebhookParseResult(WebhookConstants.InternalEvents.InvoicePaymentFailed, invoice!, e.Id, metadata);
    }

    private static WebhookParseResult HandleSubscriptionUpdated(Event e, Dictionary<string, string> metadata)
    {
        var subscription = e.Data.Object as Subscription;
        return new WebhookParseResult(WebhookConstants.InternalEvents.SubscriptionUpdated, subscription!, e.Id, metadata);
    }

    private static WebhookParseResult HandleSubscriptionDeleted(Event e, Dictionary<string, string> metadata)
    {
        var subscription = e.Data.Object as Subscription;
        return new WebhookParseResult(WebhookConstants.InternalEvents.SubscriptionDeleted, subscription!, e.Id, metadata);
    }
}

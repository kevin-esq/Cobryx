using System.Text.Json;

using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Payments.Webhooks.Common;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Application.Subscriptions.Common;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stripe;

namespace Cobryx.Infrastructure.Webhooks.Stripe
{
    /// <summary>
    /// Parses Stripe webhook payloads into provider-agnostic <see cref="WebhookParseResult" />.
    /// All Stripe-specific types are serialized to <see cref="JsonElement" /> to keep
    /// Application layer free of external payment provider dependencies.
    /// </summary>
    public partial class StripeWebhookParser(
        IOptions<StripeOptions> options,
        ILogger<StripeWebhookParser> logger) : IWebhookParser
    {
        public string Provider => "Stripe";

        public Task<WebhookParseResult> ParseAsync(string json, string? signature = null)
        {
            Event stripeEvent;

            try
            {
                StripeOptions config = options.Value;
                if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(config.WebhookSecret))
                {
                    stripeEvent = EventUtility.ParseEvent(json);
                    LogParsedWithoutSignature(logger);
                }
                else
                {
                    stripeEvent = EventUtility.ConstructEvent(json, signature, config.WebhookSecret);
                }
            }
            catch (StripeException ex)
            {
                LogSignatureVerificationFailed(logger, ex);
                throw;
            }

            Dictionary<string, string> metadata = [];
            if (!string.IsNullOrEmpty(stripeEvent.Account))
            {
                metadata["stripe_account_id"] = stripeEvent.Account;
            }

            WebhookParseResult result = stripeEvent.Type switch
            {
                StripeConstants.Events.PaymentIntentSucceeded => BuildResult(
                    WebhookConstants.InternalEvents.PaymentSucceeded, stripeEvent, metadata),
                StripeConstants.Events.PaymentIntentFailed => BuildResult(WebhookConstants.InternalEvents.PaymentFailed,
                    stripeEvent, metadata),
                StripeConstants.Events.CheckoutSessionCompleted => BuildResult(
                    WebhookConstants.InternalEvents.CheckoutCompleted, stripeEvent, metadata),
                StripeConstants.Events.InvoicePaid => BuildResult(WebhookConstants.InternalEvents.InvoicePaid,
                    stripeEvent, metadata),
                StripeConstants.Events.InvoicePaymentFailed => BuildResult(
                    WebhookConstants.InternalEvents.InvoicePaymentFailed, stripeEvent, metadata),
                StripeConstants.Events.SubscriptionUpdated => BuildResult(
                    WebhookConstants.InternalEvents.SubscriptionUpdated, stripeEvent, metadata),
                StripeConstants.Events.SubscriptionDeleted => BuildResult(
                    WebhookConstants.InternalEvents.SubscriptionDeleted, stripeEvent, metadata),
                StripeConstants.Events.ChargeRefunded => BuildResult(WebhookConstants.InternalEvents.ChargeRefunded,
                    stripeEvent, metadata),
                StripeConstants.Events.PayoutPaid => BuildResult(WebhookConstants.InternalEvents.PayoutPaid,
                    stripeEvent, metadata),
                StripeConstants.Events.PayoutFailed => BuildResult(WebhookConstants.InternalEvents.PayoutFailed,
                    stripeEvent, metadata),
                StripeConstants.Events.AccountUpdated => BuildResult(WebhookConstants.InternalEvents.AccountUpdated,
                    stripeEvent, metadata),
                _ => throw new NotSupportedException($"Stripe event type {stripeEvent.Type} is not supported.")
            };

            return Task.FromResult(result);
        }

        /// <summary>
        /// Serializes the Stripe event's data object to a <see cref="JsonElement" /> to prevent
        /// Stripe-specific types from leaking beyond the Infrastructure boundary.
        /// The event ID is used as the external transaction ID for traceability.
        /// </summary>
        private static WebhookParseResult BuildResult(string internalEventType, Event stripeEvent,
            Dictionary<string, string> metadata)
        {
            // Serialize Stripe object → JsonElement (provider-agnostic)
            var serialized = JsonSerializer.Serialize(stripeEvent.Data?.Object,
                stripeEvent.Data?.Object?.GetType() ?? typeof(object));
            JsonElement jsonData = JsonDocument.Parse(serialized).RootElement.Clone();

            return new WebhookParseResult(internalEventType, jsonData, stripeEvent.Id, metadata);
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Stripe webhook parsed without signature verification")]
        private static partial void LogParsedWithoutSignature(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Stripe webhook signature verification failed")]
        private static partial void LogSignatureVerificationFailed(ILogger logger, Exception ex);
    }
}

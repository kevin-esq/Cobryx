using System.Text.Json;

using Cobryx.Application.Payments.Services;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandlePaymentSucceeded
{
    /// <summary>
    /// Processes a payment_intent.succeeded webhook event.
    /// Extracts amount, currency, and application fee from the PaymentIntent data
    /// and delegates to <see cref="PaymentLinkReconciliationService"/> for reconciliation.
    /// </summary>
    /// <param name="Data">The PaymentIntent JSON data from Stripe.</param>
    /// <param name="WebhookEventId">The Stripe event ID for idempotency tracking.</param>
    public record HandlePaymentSucceededCommand(JsonElement Data, string WebhookEventId) : IRequest<Result>;

    public partial class HandlePaymentSucceededHandler(
        PaymentLinkReconciliationService reconciliationService,
        IProcessedWebhookEventRepository processedEventRepository,
        ILogger<HandlePaymentSucceededHandler> logger) : IRequestHandler<HandlePaymentSucceededCommand, Result>
    {
        private const string Provider = "Stripe";
        private const string EventType = "payment_intent.succeeded";

        public async Task<Result> Handle(HandlePaymentSucceededCommand request, CancellationToken cancellationToken)
        {
            JsonElement data = request.Data;

            var paymentIntentId = data.TryGetProperty("id", out JsonElement idProp)
                ? idProp.GetString()
                : null;

            if (string.IsNullOrEmpty(paymentIntentId))
            {
                LogMissingPaymentIntentId(logger);
                return Result.Failure(DomainErrorCode.Webhooks.InvalidDataFormat);
            }

            // IDEMPOTENCY CHECK: Atomic check-and-mark
            // This prevents:
            // 1. Webhook retries from Stripe
            // 2. Race between API request and webhook
            // 3. Out-of-order event processing
            var isFirstProcessor = await processedEventRepository.TryMarkAsProcessedAsync(
                Provider,
                request.WebhookEventId,
                EventType,
                paymentIntentId,
                ct: cancellationToken);

            if (!isFirstProcessor)
            {
                LogIdempotencyHit(logger, request.WebhookEventId, paymentIntentId);
                return Result.Success(); // Already processed - return success (idempotent)
            }

            var amountCents = data.TryGetProperty("amount", out JsonElement amountProp)
                ? amountProp.GetInt64()
                : 0;

            var currency = data.TryGetProperty("currency", out JsonElement currencyProp)
                ? currencyProp.GetString()?.ToUpperInvariant() ?? CobryxDefaults.Currency
                : CobryxDefaults.Currency;

            Money amount = new(amountCents / 100m, currency);

            decimal? appFee = null;
            if (data.TryGetProperty("application_fee_amount", out JsonElement feeProp) &&
                feeProp.ValueKind == JsonValueKind.Number)
            {
                appFee = feeProp.GetInt64() / 100m;
            }

            await reconciliationService.HandlePaymentSuccessAsync(paymentIntentId, amount, appFee, cancellationToken);
            return Result.Success();
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "payment_intent.succeeded event missing PaymentIntent ID")]
        private static partial void LogMissingPaymentIntentId(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Idempotency hit for webhook {EventId} (PaymentIntent: {PaymentIntentId})")]
        private static partial void LogIdempotencyHit(ILogger logger, string eventId, string paymentIntentId);
    }
}

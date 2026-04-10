using System.Text.Json;

using Cobryx.Application.Payments.Commands.RefundPayment;
using Cobryx.Application.Payments.Services;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleChargeRefunded
{
    /// <summary>
    /// Processes a charge.refunded webhook event.
    /// Executes BOTH refund flows:
    ///   1. Invoice/payment refund via <see cref="RefundPaymentCommand"/> (if payment exists by reference)
    ///   2. PaymentLink refund via <see cref="PaymentLinkReconciliationService"/> (ledger reversal)
    /// These are two distinct business flows that must coexist — invoice refunds and PaymentLink refunds.
    /// </summary>
    public record HandleChargeRefundedCommand(JsonElement Data, string StripeEventId) : IRequest<Result>;

    public partial class HandleChargeRefundedHandler(
        IPaymentRepository paymentRepository,
        PaymentLinkReconciliationService reconciliationService,
        ISender sender,
        ILogger<HandleChargeRefundedHandler> logger) : IRequestHandler<HandleChargeRefundedCommand, Result>
    {
        public async Task<Result> Handle(HandleChargeRefundedCommand request, CancellationToken cancellationToken)
        {
            JsonElement data = request.Data;

            var paymentIntentId = data.TryGetProperty("payment_intent", out JsonElement piProp)
                ? piProp.GetString()
                : null;

            if (string.IsNullOrEmpty(paymentIntentId))
            {
                LogMissingPaymentIntentId(logger, request.StripeEventId);
                return Result.Success();
            }

            var refundedCents = data.TryGetProperty("amount_refunded", out JsonElement amountProp)
                ? amountProp.GetInt64()
                : 0;

            var currency = data.TryGetProperty("currency", out JsonElement currencyProp)
                ? currencyProp.GetString()?.ToUpperInvariant() ?? CobryxDefaults.Currency
                : CobryxDefaults.Currency;

            Money refundAmount = new(refundedCents / 100m, currency);

            Domain.Payments.Payment? payment =
                await paymentRepository.GetByReferenceAsync(paymentIntentId, cancellationToken);
            if (payment != null)
            {
                LogInvoiceRefund(logger, payment.Id, paymentIntentId);
                Result refundResult = await sender.Send(
                    new RefundPaymentCommand(payment.Id, refundAmount.Amount, currency), cancellationToken);

                if (refundResult.IsFailure)
                {
                    LogRefundFailed(logger, payment.Id, refundResult.Error ?? "unknown");
                }
            }

            LogPaymentLinkRefund(logger, paymentIntentId);
            await reconciliationService.HandleRefundAsync(paymentIntentId, refundAmount, cancellationToken);

            return Result.Success();
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "charge.refunded event {StripeEventId} missing payment_intent. Skipping.")]
        private static partial void LogMissingPaymentIntentId(ILogger logger, string stripeEventId);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Processing invoice refund for Payment {PaymentId} (PI: {PaymentIntentId})")]
        private static partial void LogInvoiceRefund(ILogger logger, Guid paymentId, string paymentIntentId);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Invoice refund failed for Payment {PaymentId}: {Error}")]
        private static partial void LogRefundFailed(ILogger logger, Guid paymentId, string error);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Processing PaymentLink refund for PI: {PaymentIntentId}")]
        private static partial void LogPaymentLinkRefund(ILogger logger, string paymentIntentId);
    }
}

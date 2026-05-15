using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Operations.Commands.RecordPayout;
using Cobryx.Application.Payments.Commands.HandleChargeback;
using Cobryx.Application.Payments.Webhooks.Commands.HandleAccountUpdated;
using Cobryx.Application.Payments.Webhooks.Commands.HandleChargeRefunded;
using Cobryx.Application.Payments.Webhooks.Commands.HandleCheckoutCompleted;
using Cobryx.Application.Payments.Webhooks.Commands.HandleInvoicePaid;
using Cobryx.Application.Payments.Webhooks.Commands.HandlePaymentFailed;
using Cobryx.Application.Payments.Webhooks.Commands.HandlePaymentSucceeded;
using Cobryx.Application.Payments.Webhooks.Commands.HandleSubscriptionChanged;
using Cobryx.Application.Payments.Webhooks.Common;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleWebhookEvent
{
    public record HandleWebhookEventCommand(Guid WebhookEventId) : IRequest<Result>;

    public partial class HandleWebhookEventHandler(
        IWebhookEventRepository webhookEventRepository,
        IPaymentRepository paymentRepository,
        ITenantRepository tenantRepository,
        IEnumerable<IWebhookParser> parsers,
        ISender sender,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<HandleWebhookEventHandler> logger) : IRequestHandler<HandleWebhookEventCommand, Result>
    {
        public async Task<Result> Handle(HandleWebhookEventCommand request, CancellationToken cancellationToken)
        {
            WebhookEvent? webhookEvent =
                await webhookEventRepository.GetByIdAsync(request.WebhookEventId, cancellationToken);

            if (webhookEvent == null)
            {
                return Result.Failure(DomainErrorCode.Webhooks.EventNotFound);
            }

            if (webhookEvent.Status == WebhookStatus.Processed)
            {
                return Result.Success();
            }

            IWebhookParser? parser = parsers.FirstOrDefault(p =>
                p.Provider.Equals(webhookEvent.Provider, StringComparison.OrdinalIgnoreCase));
            if (parser == null)
            {
                webhookEvent.MarkAsFailed(DomainErrorCode.Webhooks.ParserNotFound);
                _ = await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure(DomainErrorCode.Webhooks.ParserNotFound);
            }

            webhookEvent.StartProcessing(clock.UtcNow);
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                WebhookParseResult parseResult = await parser.ParseAsync(webhookEvent.RawPayload);
                Result result = await DispatchInternalCommandAsync(parseResult, cancellationToken);

                if (result.IsSuccess)
                {
                    webhookEvent.MarkAsProcessed(clock.UtcNow);
                }
                else
                {
                    webhookEvent.MarkAsFailed(result.Error ?? CobryxDefaults.UnknownValue);
                }
            }
            catch (Exception ex)
            {
                LogProcessingError(logger, ex, webhookEvent.Id);
                webhookEvent.MarkAsFailed(ex.Message);
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        private async Task<Result> DispatchInternalCommandAsync(WebhookParseResult parseResult, CancellationToken ct)
        {
            if (parseResult.Data is not JsonElement data)
            {
                return Result.Failure(DomainErrorCode.Webhooks.InvalidDataFormat);
            }

            var eventId = parseResult.ExternalTransactionId ?? "unknown";

            return parseResult.InternalEventType switch
            {
                // Subscription lifecycle
                WebhookConstants.InternalEvents.CheckoutCompleted
                    => await sender.Send(new HandleCheckoutCompletedCommand(data, eventId), ct),

                WebhookConstants.InternalEvents.InvoicePaid
                    => await sender.Send(new HandleInvoicePaidCommand(data, eventId), ct),

                WebhookConstants.InternalEvents.SubscriptionUpdated
                    => await sender.Send(new HandleSubscriptionChangedCommand(data, eventId, IsDeleted: false), ct),

                WebhookConstants.InternalEvents.SubscriptionDeleted
                    => await sender.Send(new HandleSubscriptionChangedCommand(data, eventId, IsDeleted: true), ct),

                // Payment processing
                WebhookConstants.InternalEvents.PaymentSucceeded
                    => await sender.Send(new HandlePaymentSucceededCommand(data, eventId), ct),

                WebhookConstants.InternalEvents.PaymentFailed
                    => await sender.Send(new HandlePaymentFailedCommand(data, parseResult.InternalEventType), ct),

                WebhookConstants.InternalEvents.InvoicePaymentFailed
                    => await sender.Send(new HandlePaymentFailedCommand(data, parseResult.InternalEventType), ct),

                // Refunds & disputes
                WebhookConstants.InternalEvents.ChargeRefunded
                    => await sender.Send(new HandleChargeRefundedCommand(data, eventId), ct),

                WebhookConstants.InternalEvents.ChargeDisputeCreated
                    => await HandleChargebackAsync(parseResult, ct),

                // Payouts
                WebhookConstants.InternalEvents.PayoutPaid
                    => await HandlePayoutAsync(parseResult, data, ct),

                WebhookConstants.InternalEvents.PayoutFailed
                    => await HandlePayoutAsync(parseResult, data, ct),

                // Connect
                WebhookConstants.InternalEvents.AccountUpdated
                    => await sender.Send(new HandleAccountUpdatedCommand(data), ct),

                _ => HandleUnknownEvent(parseResult)
            };
        }

        /// <summary>
        /// Handles payout.paid and payout.failed events.
        /// Resolves tenant by Stripe account ID and records the payout.
        /// </summary>
        private async Task<Result> HandlePayoutAsync(WebhookParseResult parseResult, JsonElement data,
            CancellationToken ct)
        {
            var stripeAccountId = parseResult.Metadata != null &&
                                  parseResult.Metadata.TryGetValue("stripe_account_id", out var accountId)
                ? accountId
                : null;

            Guid tenantId = Guid.Empty;

            if (!string.IsNullOrEmpty(stripeAccountId))
            {
                Domain.Identity.Tenant? tenant = await tenantRepository.GetByStripeAccountIdAsync(stripeAccountId, ct);
                tenantId = tenant?.Id ?? Guid.Empty;
            }

            if (tenantId == Guid.Empty)
            {
                LogTenantNotFound(logger, stripeAccountId ?? "Missing");
                return Result.Failure(DomainErrorCode.Tenant.NotFound);
            }

            var amount = data.GetProperty("amount").GetInt64() / 100m;
            var currency = data.GetProperty("currency").GetString()?.ToUpperInvariant() ?? CobryxDefaults.Currency;
            var status = data.GetProperty("status").GetString() ?? "unknown";

            RecordPayoutCommand command = new(tenantId, amount, currency,
                parseResult.ExternalTransactionId ?? "unknown", status);
            return await sender.Send(command, ct);
        }

        /// <summary>
        /// Handles charge.dispute.created events.
        /// Looks up the payment by external reference and creates a chargeback record.
        /// </summary>
        private async Task<Result> HandleChargebackAsync(WebhookParseResult parseResult, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(parseResult.ExternalTransactionId))
            {
                return Result.Failure(DomainErrorCode.Webhooks.MissingTransactionId);
            }

            Domain.Payments.Payment? payment =
                await paymentRepository.GetByReferenceAsync(parseResult.ExternalTransactionId, ct);
            if (payment == null)
            {
                return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);
            }

            HandleChargebackCommand command = new(payment.Id);
            return await sender.Send(command, ct);
        }

        private Result HandleUnknownEvent(WebhookParseResult parseResult)
        {
            LogUnknownEvent(logger, parseResult.InternalEventType);
            return Result.Failure(DomainErrorCode.System.NotAllowed);
        }

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Error processing webhook {WebhookEventId}")]
        private static partial void LogProcessingError(ILogger logger, Exception ex, Guid webhookEventId);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Could not resolve Tenant for Stripe Account {StripeAccountId}")]
        private static partial void LogTenantNotFound(ILogger logger, string stripeAccountId);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Translation for event type {InternalEventType} not implemented")]
        private static partial void LogUnknownEvent(ILogger logger, string internalEventType);
    }
}

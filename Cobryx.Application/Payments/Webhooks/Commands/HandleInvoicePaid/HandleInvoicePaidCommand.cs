using System.Text.Json;

using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleInvoicePaid
{
    /// <summary>
    /// Processes an invoice.paid webhook event.
    /// Extracts the subscription ID from the invoice data and delegates
    /// to <see cref="StripeSubscriptionSyncService"/> for authoritative state sync.
    /// </summary>
    public record HandleInvoicePaidCommand(JsonElement Data, string StripeEventId) : IRequest<Result>;

    public partial class HandleInvoicePaidHandler(
        StripeSubscriptionSyncService syncService,
        ILogger<HandleInvoicePaidHandler> logger) : IRequestHandler<HandleInvoicePaidCommand, Result>
    {
        public async Task<Result> Handle(HandleInvoicePaidCommand request, CancellationToken cancellationToken)
        {
            JsonElement data = request.Data;

            var subscriptionId = data.TryGetProperty("subscription", out JsonElement subProp)
                ? subProp.GetString()
                : null;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                LogMissingSubscriptionId(logger, request.StripeEventId);
                return Result.Success();
            }

            await syncService.HandleInvoicePaidAsync(request.StripeEventId, subscriptionId, cancellationToken);
            return Result.Success();
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "invoice.paid event {StripeEventId} missing subscription ID. Skipping.")]
        private static partial void LogMissingSubscriptionId(ILogger logger, string stripeEventId);
    }
}

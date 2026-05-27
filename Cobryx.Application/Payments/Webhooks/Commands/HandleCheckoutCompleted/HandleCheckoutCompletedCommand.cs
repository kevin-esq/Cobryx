using System.Text.Json;

using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleCheckoutCompleted
{
    /// <summary>
    /// Processes a checkout.session.completed webhook event.
    /// Extracts tenant, customer, and subscription identifiers from the session metadata
    /// and delegates to <see cref="StripeSubscriptionSyncService"/> for authoritative state sync.
    /// </summary>
    [WebhookSystem]
public record HandleCheckoutCompletedCommand(JsonElement Data, string StripeEventId) : IRequest<Result>;

    public partial class HandleCheckoutCompletedHandler(
        StripeSubscriptionSyncService syncService,
        ILogger<HandleCheckoutCompletedHandler> logger) : IRequestHandler<HandleCheckoutCompletedCommand, Result>
    {
        public async Task<Result> Handle(HandleCheckoutCompletedCommand request, CancellationToken cancellationToken)
        {
            JsonElement data = request.Data;

            if (!data.TryGetProperty("metadata", out JsonElement metadata) ||
                !metadata.TryGetProperty(CobryxClaimTypes.TenantId, out JsonElement tenantIdProp) ||
                !Guid.TryParse(tenantIdProp.GetString(), out Guid tenantId))
            {
                LogMissingTenantId(logger, request.StripeEventId);
                return Result.Success();
            }

            var stripeCustomerId = data.TryGetProperty("customer", out JsonElement customerProp)
                ? customerProp.GetString()
                : null;

            var stripeSubscriptionId = data.TryGetProperty("subscription", out JsonElement subProp)
                ? subProp.GetString()
                : null;

            if (string.IsNullOrEmpty(stripeCustomerId) || string.IsNullOrEmpty(stripeSubscriptionId))
            {
                LogMissingIds(logger, request.StripeEventId);
                return Result.Success();
            }

            await syncService.HandleCheckoutCompletedAsync(
                request.StripeEventId, stripeCustomerId, stripeSubscriptionId, tenantId, cancellationToken);

            return Result.Success();
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Checkout completed event {StripeEventId} missing tenant_id in metadata. Skipping.")]
        private static partial void LogMissingTenantId(ILogger logger, string stripeEventId);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Checkout completed event {StripeEventId} missing customer or subscription ID. Skipping.")]
        private static partial void LogMissingIds(ILogger logger, string stripeEventId);
    }
}

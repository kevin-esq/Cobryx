using System.Text.Json;

using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleSubscriptionChanged
{
    /// <summary>
    /// Processes customer.subscription.updated and customer.subscription.deleted webhook events.
    /// Extracts the Stripe subscription ID and delegates to <see cref="StripeSubscriptionSyncService"/>
    /// for authoritative state synchronization.
    /// </summary>
    public record HandleSubscriptionChangedCommand(
        JsonElement Data,
        string StripeEventId,
        bool IsDeleted) : IRequest<Result>;

    public partial class HandleSubscriptionChangedHandler(
        StripeSubscriptionSyncService syncService,
        ILogger<HandleSubscriptionChangedHandler> logger) : IRequestHandler<HandleSubscriptionChangedCommand, Result>
    {
        public async Task<Result> Handle(HandleSubscriptionChangedCommand request, CancellationToken cancellationToken)
        {
            JsonElement data = request.Data;

            var subscriptionId = data.TryGetProperty("id", out JsonElement idProp)
                ? idProp.GetString()
                : null;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                LogMissingSubscriptionId(logger, request.StripeEventId);
                return Result.Failure(DomainErrorCode.Webhooks.InvalidDataFormat);
            }

            if (request.IsDeleted)
            {
                await syncService.HandleSubscriptionDeletedAsync(request.StripeEventId, subscriptionId,
                    cancellationToken);
            }
            else
            {
                await syncService.HandleSubscriptionUpdatedAsync(request.StripeEventId, subscriptionId,
                    cancellationToken);
            }

            return Result.Success();
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Subscription changed event {StripeEventId} missing subscription ID")]
        private static partial void LogMissingSubscriptionId(ILogger logger, string stripeEventId);
    }
}

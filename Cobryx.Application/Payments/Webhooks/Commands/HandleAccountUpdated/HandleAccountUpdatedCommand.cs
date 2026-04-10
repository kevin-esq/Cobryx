using System.Text.Json;

using Cobryx.Application.Tenants.Commands.UpdateTenantConnectCapabilities;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleAccountUpdated
{
    /// <summary>
    /// Processes an account.updated webhook event from Stripe Connect.
    /// Extracts Connect capability flags and delegates to
    /// <see cref="UpdateTenantConnectCapabilitiesCommand"/> for tenant state sync.
    /// </summary>
    public record HandleAccountUpdatedCommand(JsonElement Data) : IRequest<Result>;

    public partial class HandleAccountUpdatedHandler(
        ISender sender,
        ILogger<HandleAccountUpdatedHandler> logger) : IRequestHandler<HandleAccountUpdatedCommand, Result>
    {
        public async Task<Result> Handle(HandleAccountUpdatedCommand request, CancellationToken cancellationToken)
        {
            JsonElement data = request.Data;

            var stripeAccountId = data.TryGetProperty("id", out JsonElement idProp)
                ? idProp.GetString()
                : null;

            if (string.IsNullOrEmpty(stripeAccountId))
            {
                LogMissingAccountId(logger);
                return Result.Failure(DomainErrorCode.Webhooks.InvalidDataFormat);
            }

            var chargesEnabled = data.TryGetProperty("charges_enabled", out JsonElement chargesProp) &&
                                 chargesProp.GetBoolean();
            var payoutsEnabled = data.TryGetProperty("payouts_enabled", out JsonElement payoutsProp) &&
                                 payoutsProp.GetBoolean();
            var detailsSubmitted = data.TryGetProperty("details_submitted", out JsonElement detailsProp) &&
                                   detailsProp.GetBoolean();

            LogProcessing(logger, stripeAccountId);

            Result result = await sender.Send(
                new UpdateTenantConnectCapabilitiesCommand(stripeAccountId, chargesEnabled, payoutsEnabled,
                    detailsSubmitted),
                cancellationToken);

            return result;
        }

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "account.updated event missing Stripe account ID")]
        private static partial void LogMissingAccountId(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Processing account.updated for Stripe account {StripeAccountId}")]
        private static partial void LogProcessing(ILogger logger, string stripeAccountId);
    }
}

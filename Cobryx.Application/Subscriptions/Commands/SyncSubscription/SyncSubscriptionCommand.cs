using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Subscriptions.Commands.SyncSubscription
{
    [TenantScoped]
public record SyncSubscriptionCommand : IRequest<Result>, IRequiresTenant;

    public class SyncSubscriptionHandler(
        ITenantProvider tenantProvider,
        StripeSubscriptionSyncService syncService,
        ITenantSubscriptionRepository subscriptionRepository,
        IClock clock,
        ILogger<SyncSubscriptionHandler> logger) : IRequestHandler<SyncSubscriptionCommand, Result>
    {
        private readonly ITenantProvider _tenantProvider = tenantProvider;
        private readonly StripeSubscriptionSyncService _syncService = syncService;
        private readonly ITenantSubscriptionRepository _subscriptionRepository = subscriptionRepository;
        private readonly IClock _clock = clock;
        private readonly ILogger<SyncSubscriptionHandler> _logger = logger;

        public async Task<Result> Handle(SyncSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure(DomainErrorCode.Tenant.ContextMissing);
            }

            _logger.LogInformation("Initiating manual Stripe sync for Tenant {TenantId}", tenantId);

            var subscription = await _subscriptionRepository.GetByTenantIdWithLockAsync(tenantId.Value, cancellationToken);

            if (subscription == null)
            {
                _logger.LogWarning("Manual sync failed: No subscription record found for Tenant {TenantId}", tenantId);
                return Result.Failure(DomainErrorCode.Subscription.NotFound);
            }

            if (string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
            {
                _logger.LogWarning("Manual sync skipped: Tenant {TenantId} has no associated Stripe Subscription ID", tenantId);
                return Result.Failure(DomainErrorCode.Subscription.NotActiveInStripe);
            }

            var syntheticEventId = $"manual_sync_{_clock.UtcNow.Ticks}";

            await _syncService.SyncAuthoritativeStateAsync(
                syntheticEventId,
                subscription.StripeSubscriptionId,
                subscription,
                StripeConstants.Events.ManualSyncRequested,
                cancellationToken);

            _logger.LogInformation("Manual Stripe sync completed successfully for Tenant {TenantId}", tenantId);

            return Result.Success();
        }
    }
}

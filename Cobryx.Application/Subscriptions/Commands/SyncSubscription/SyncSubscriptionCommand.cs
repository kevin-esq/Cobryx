using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Subscriptions.Commands.SyncSubscription;

public record SyncSubscriptionCommand : IRequest<Result>;

public class SyncSubscriptionHandler : IRequestHandler<SyncSubscriptionCommand, Result>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly StripeSubscriptionSyncService _syncService;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SyncSubscriptionHandler> _logger;

    public SyncSubscriptionHandler(
        ITenantProvider tenantProvider,
        StripeSubscriptionSyncService syncService,
        ITenantSubscriptionRepository subscriptionRepository,
        ILogger<SyncSubscriptionHandler> logger)
    {
        _tenantProvider = tenantProvider;
        _syncService = syncService;
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(SyncSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        _logger.LogInformation("Initiating manual Stripe sync for Tenant {TenantId}", tenantId);

        // Lock the subscription row to prevent race conditions during sync
        // Implementation is delegated to the repository to handle provider-specific locking (e.g., FOR UPDATE)
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

        // We use a synthetic event ID for manual sync to avoid collision with webhooks
        var syntheticEventId = $"manual_sync_{DateTime.UtcNow.Ticks}";

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

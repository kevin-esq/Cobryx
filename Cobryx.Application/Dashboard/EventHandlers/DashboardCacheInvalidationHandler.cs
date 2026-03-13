using Cobryx.Application.Common.Events;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Events;
using Cobryx.Domain.Events.Payments;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Dashboard.EventHandlers;

public class DashboardCacheInvalidationHandler :
    INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>,
    INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<DashboardCacheInvalidationHandler> _logger;

    public DashboardCacheInvalidationHandler(ICacheService cacheService, ILogger<DashboardCacheInvalidationHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        await InvalidateCache(notification.DomainEvent.TenantId);
    }

    public async Task Handle(DomainEventNotification<UserRegisteredEvent> notification, CancellationToken cancellationToken)
    {
        await InvalidateCache(notification.DomainEvent.TenantId);
    }

    private async Task InvalidateCache(Guid tenantId)
    {
        var cacheKey = $"dash:{tenantId}:summary";
        _logger.LogInformation("Invalidating dashboard cache for tenant {TenantId}.", tenantId);
        await _cacheService.RemoveAsync(cacheKey);
    }
}

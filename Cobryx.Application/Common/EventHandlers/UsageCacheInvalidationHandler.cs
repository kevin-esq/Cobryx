using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events;
using Cobryx.Domain.Events.Invoicing;
using Cobryx.Domain.Events.Lending;
using Concordia;

namespace Cobryx.Application.Common.EventHandlers;

public class UsageCacheInvalidationHandler : 
    INotificationHandler<DomainEventNotification<InvoiceIssuedEvent>>,
    INotificationHandler<DomainEventNotification<UserRegisteredEvent>>,
    INotificationHandler<DomainEventNotification<LoanCreatedEvent>>
{
    private readonly ICacheService _cacheService;

    public UsageCacheInvalidationHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task Handle(DomainEventNotification<InvoiceIssuedEvent> notification, CancellationToken ct)
    {
        await InvalidateCache(notification.DomainEvent.TenantId, ct);
    }

    public async Task Handle(DomainEventNotification<UserRegisteredEvent> notification, CancellationToken ct)
    {
        await InvalidateCache(notification.DomainEvent.TenantId, ct);
    }

    public async Task Handle(DomainEventNotification<LoanCreatedEvent> notification, CancellationToken ct)
    {
        await InvalidateCache(notification.DomainEvent.TenantId, ct);
    }

    private async Task InvalidateCache(Guid tenantId, CancellationToken ct)
    {
        string cacheKey = IUsageMeteringService.GetCacheKey(tenantId);
        await _cacheService.RemoveAsync(cacheKey, ct);
    }
}

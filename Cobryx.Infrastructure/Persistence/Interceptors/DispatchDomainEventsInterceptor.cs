using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventService _domainEventService;

    public DispatchDomainEventsInterceptor(IDomainEventService domainEventService)
    {
        _domainEventService = domainEventService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        DispatchEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await DispatchEvents(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchEvents(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context == null) return;

        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _domainEventService.Publish(domainEvent);
        }
    }
}

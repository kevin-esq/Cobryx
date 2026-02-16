using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

public class OutboxInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureEvents(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureEvents(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context == null) return;

        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entities.Any()) return;

        var outboxEvents = entities
            .SelectMany(e =>
            {
                var domainEvents = e.DomainEvents.ToList();
                e.ClearDomainEvents();
                return domainEvents;
            })
            .Select(domainEvent =>
            {
                var outboxEvent = new OutboxEvent(
                    domainEvent.GetType().FullName!,
                    JsonConvert.SerializeObject(domainEvent, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }));

                // Capture CorrelationId from the ambient Activity
                outboxEvent.SetCorrelationId(Activity.Current?.GetTagItem("CorrelationId")?.ToString()
                                           ?? Activity.Current?.Id);

                return outboxEvent;
            })
            .ToList();

        context.Set<OutboxEvent>().AddRange(outboxEvents);
    }
}

using System.Diagnostics;

using Cobryx.Domain.Messaging;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Newtonsoft.Json;

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

    private static void CaptureEvents(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context == null) return;

        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        if (entities.Count == 0) return;

        var outboxEvents = entities
            .SelectMany(e =>
            {
                var domainEvents = e.DomainEvents.ToList();
                e.ClearDomainEvents();
                return domainEvents.Select(domainEvent => new { Entity = e, Event = domainEvent });
            })
            .Select(pair =>
            {
                var e = pair.Entity;
                var domainEvent = pair.Event;

                var correlationId = Activity.Current?.GetTagItem("CorrelationId")?.ToString()
                                    ?? Activity.Current?.Id;

                var tenantId = e is ITenantEntity te ? te.TenantId : Guid.Empty;

                var outboxMsg = new OutboxMessage(
                    tenantId,
                    domainEvent.GetType().FullName!,
                    JsonConvert.SerializeObject(domainEvent, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }),
                    correlationId);

                return outboxMsg;
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxEvents);
    }
}

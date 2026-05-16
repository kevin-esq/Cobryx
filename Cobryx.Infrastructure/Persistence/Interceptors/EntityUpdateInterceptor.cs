using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

public class EntityUpdateInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null)
            return;

        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedAt).CurrentValue = now;
                entry.Entity.UpdateTimestamp(now);
            }

            // Only bump optimistic concurrency token on real updates. Doing this on Added breaks SQLite
            // (and retries): the row is new and the in-memory Version no longer matches what the provider tracks.
            if (entry.State == EntityState.Modified || entry.HasChangedOwnedAuditedEntities())
            {
                entry.Entity.UpdateTimestamp(now);
                entry.Entity.IncrementVersion();
            }
        }
    }
}

public static class EntityEntryExtensions
{
    public static bool HasChangedOwnedAuditedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            r.TargetEntry.State is EntityState.Added or EntityState.Modified);
}

using System.Text.Json;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ITenantProvider _tenantProvider;

    public AuditInterceptor(ICurrentUserProvider currentUserProvider, ITenantProvider tenantProvider)
    {
        _currentUserProvider = currentUserProvider;
        _tenantProvider = tenantProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        OnSavingChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        OnSavingChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void OnSavingChanges(DbContext? context)
    {
        if (context == null) return;

        var userId = _currentUserProvider.GetUserId();
        var tenantId = _tenantProvider.GetTenantId();

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // 1. System Metadata (CreatedBy/UpdatedBy)
            if (entry.State == EntityState.Added)
            {
                if (userId.HasValue) entry.Entity.SetCreatedBy(userId.Value);
            }
            else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                if (userId.HasValue) entry.Entity.SetUpdatedBy(userId.Value);
                else entry.Entity.UpdateTimestamp();
            }

            // 2. Audit Log Detection
            if (tenantId.HasValue && entry.Entity is not AuditLog && entry.Entity is not SystemErrorLog)
            {
                var auditEntry = new AuditEntry(entry)
                {
                    TenantId = tenantId.Value,
                    UserId = userId,
                    EntityName = entry.Entity.GetType().Name,
                    Action = entry.State.ToString()
                };
                auditEntries.Add(auditEntry);
            }
        }

        // Save audit entries
        foreach (var auditEntry in auditEntries)
        {
            context.Set<AuditLog>().Add(auditEntry.ToAuditLog());
        }
    }
}

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string EntityName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();

    public AuditLog ToAuditLog()
    {
        var entityId = Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "Unknown";

        foreach (var property in Entry.Properties)
        {
            string propertyName = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey()) continue;

            switch (Entry.State)
            {
                case EntityState.Added:
                    NewValues[propertyName] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    OldValues[propertyName] = property.OriginalValue;
                    break;

                case EntityState.Modified:
                    if (property.IsModified)
                    {
                        OldValues[propertyName] = property.OriginalValue;
                        NewValues[propertyName] = property.CurrentValue;
                    }
                    break;
            }
        }

        return new AuditLog(
            TenantId,
            UserId,
            EntityName,
            entityId,
            Action,
            OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues)
        );
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}

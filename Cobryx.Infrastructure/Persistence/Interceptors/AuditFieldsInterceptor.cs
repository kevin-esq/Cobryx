using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

public class AuditFieldsInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            // EF sometimes tracks new LoginSession rows as Modified (graph fix-up), which would emit UPDATEs
            // against non-existent rows. Only coerce those — never User or other roots at Version 0.
            if (entry.State == EntityState.Modified && entry.Entity.Version == 0 && entry.Entity is LoginSession)
            {
                entry.State = EntityState.Added;
            }

            // Do not bump Version here: EntityUpdateInterceptor already increments on Modified, and
            // bumping on Added breaks optimistic concurrency (e.g. LoginSession INSERT → UPDATE mismatch on SQLite).
        }
    }
}

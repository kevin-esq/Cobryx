using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class TenantSubscriptionRepository : BaseRepository<TenantSubscription>, ITenantSubscriptionRepository
{
    public TenantSubscriptionRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<TenantSubscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _dbContext.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
    }

    public async Task<TenantSubscription?> GetByTenantIdWithLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Explicitly using FOR UPDATE to prevent race conditions during authoritative Stripe sync
        return await _dbContext.TenantSubscriptions
            .FromSqlRaw("SELECT * FROM \"TenantSubscriptions\" WHERE \"TenantId\" = {0} FOR UPDATE", tenantId)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(ct);
    }
}

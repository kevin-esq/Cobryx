using Cobryx.Domain.Entities;
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
}

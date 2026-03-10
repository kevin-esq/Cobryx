using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<Tenant?> GetByStripeAccountIdAsync(string stripeAccountId, CancellationToken ct = default)
    {
        return await _dbContext.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.StripeAccountId == stripeAccountId, ct);
    }
}

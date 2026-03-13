using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class SubscriptionPlanRepository : BaseRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<SubscriptionPlan>> GetActivePlansAsync(CancellationToken ct = default)
    {
        return await _dbContext.SubscriptionPlans
            .Where(p => p.IsActive)
            .ToListAsync(ct);
    }
}

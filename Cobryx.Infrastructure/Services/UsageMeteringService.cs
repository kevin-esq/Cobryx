using Cobryx.Application.Common.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services;

public class UsageMeteringService : IUsageMeteringService
{
    private readonly CobryxDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public UsageMeteringService(CobryxDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<UsageSnapshot> GetUsageSnapshotAsync(Guid tenantId, CancellationToken ct = default)
    {
        var cacheKey = IUsageMeteringService.GetCacheKey(tenantId);
        var cachedSnapshot = await _cacheService.GetAsync<UsageSnapshot>(cacheKey, ct);

        if (cachedSnapshot != null)
        {
            return cachedSnapshot;
        }

        var invoicesCount = await _dbContext.Invoices
            .CountAsync(i => i.TenantId == tenantId, ct);

        var activeUsersCount = await _dbContext.Users
            .CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);

        var activeLoansCount = await _dbContext.Loans
            .CountAsync(l => l.TenantId == tenantId && !l.IsDeleted, ct);

        var subscription = await _dbContext.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        var snapshot = new UsageSnapshot(
            invoicesCount,
            activeUsersCount,
            activeLoansCount,
            subscription?.Plan?.MaxInvoices ?? 0,
            subscription?.Plan?.MaxUsers ?? 0,
            subscription?.Plan?.MaxLoans ?? 0);

        // Store in cache for 5 minutes. Snapshots are reactive-invalidated by Domain Events.
        await _cacheService.SetAsync(cacheKey, snapshot, TimeSpan.FromMinutes(5), ct);

        return snapshot;
    }
}

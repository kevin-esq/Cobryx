using Cobryx.Application.Common.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services;

public class UsageMeteringService : IUsageMeteringService
{
    private readonly CobryxDbContext _dbContext;

    public UsageMeteringService(CobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UsageSnapshot> GetUsageSnapshotAsync(Guid tenantId, CancellationToken ct = default)
    {
        var invoicesCount = await _dbContext.Invoices
            .CountAsync(i => i.TenantId == tenantId, ct);

        var activeUsersCount = await _dbContext.Users
            .CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);

        var activeLoansCount = await _dbContext.Loans
            .CountAsync(l => l.TenantId == tenantId && !l.IsDeleted, ct);

        var subscription = await _dbContext.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        return new UsageSnapshot(
            invoicesCount,
            activeUsersCount,
            activeLoansCount,
            subscription?.Plan?.MaxInvoices ?? 0,
            subscription?.Plan?.MaxUsers ?? 0);
    }
}

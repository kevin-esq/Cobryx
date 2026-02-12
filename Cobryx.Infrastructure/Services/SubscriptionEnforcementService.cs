using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Exceptions;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services;

public class SubscriptionEnforcementService : ISubscriptionEnforcementService
{
    private readonly IUsageMeteringService _usageMeteringService;
    private readonly CobryxDbContext _dbContext;

    public SubscriptionEnforcementService(
        IUsageMeteringService usageMeteringService,
        CobryxDbContext dbContext)
    {
        _usageMeteringService = usageMeteringService;
        _dbContext = dbContext;
    }

    public async Task EnsureWithinInvoicesLimitAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnsureSubscriptionActiveAsync(tenantId, ct);
        
        var usage = await _usageMeteringService.GetUsageSnapshotAsync(tenantId, ct);
        if (usage.InvoicesCount >= usage.MaxInvoices)
        {
            throw SubscriptionLimitExceededException.LimitReached("Invoices");
        }
    }

    public async Task EnsureWithinUsersLimitAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnsureSubscriptionActiveAsync(tenantId, ct);

        var usage = await _usageMeteringService.GetUsageSnapshotAsync(tenantId, ct);
        if (usage.ActiveUsersCount >= usage.MaxUsers)
        {
            throw SubscriptionLimitExceededException.LimitReached("Users");
        }
    }

    public async Task EnsureWithinLoansLimitAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnsureSubscriptionActiveAsync(tenantId, ct);

        var usage = await _usageMeteringService.GetUsageSnapshotAsync(tenantId, ct);
        if (usage.ActiveLoansCount >= 999999) // Placeholder if no loan limit defined in plan yet
        {
            throw SubscriptionLimitExceededException.LimitReached("Loans");
        }
    }

    public async Task EnsureSubscriptionActiveAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Use FOR UPDATE (Postgres) to lock the subscription row for this tenant.
        // This serializes all limit-checking requests for the same tenant, preventing race conditions.
        var subscription = await _dbContext.TenantSubscriptions
            .FromSqlRaw("SELECT * FROM \"TenantSubscriptions\" WHERE \"TenantId\" = {0} FOR UPDATE", tenantId)
            .FirstOrDefaultAsync(ct);

        if (subscription == null)
        {
            throw SubscriptionLimitExceededException.Blocked();
        }

        var now = DateTime.UtcNow;

        if (subscription.IsBlocked(now))
        {
            throw SubscriptionLimitExceededException.Blocked();
        }

        if (subscription.IsExpired(now))
        {
            throw SubscriptionLimitExceededException.Expired();
        }
    }
}

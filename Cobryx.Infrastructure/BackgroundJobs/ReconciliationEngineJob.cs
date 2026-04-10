using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Continuous security net for financial integrity.
/// Scans all active tenants for ledger/Stripe discrepancies.
/// </summary>
public class ReconciliationEngineJob(
    ICobryxDbContext dbContext,
    ReconciliationEngine reconciliationEngine,
    ILogger<ReconciliationEngineJob> logger)
{
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly ReconciliationEngine _reconciliationEngine = reconciliationEngine;
    private readonly ILogger<ReconciliationEngineJob> _logger = logger;

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Reconciliation Job: Starting global scan.");

        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsConnectActive || t.Id == CobryxDefaults.PlatformTenantId)
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                var to = DateTime.UtcNow.AddMinutes(-5);
                var from = await GetLastReconciliationPointAsync(tenant.Id, ct);

                _logger.LogInformation("Reconciling Tenant {TenantId} from {From} to {To}", tenant.Id, from, to);

                await _reconciliationEngine.ReconcileAsync(
                    tenant.Id,
                    from,
                    to,
                    tenant.Id == CobryxDefaults.PlatformTenantId ? null : tenant.StripeAccountId,
                    ct
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation Job: Failed for Tenant {TenantId}", tenant.Id);
            }
        }
    }

    private async Task<DateTime> GetLastReconciliationPointAsync(Guid tenantId, CancellationToken ct)
    {
        var lastRun = await _dbContext.ReconciliationAudits
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Status == ReconciliationStatus.Synced)
            .OrderByDescending(a => a.ToUtc)
            .Select(a => a.ToUtc)
            .FirstOrDefaultAsync(ct);

        return lastRun == default ? DateTime.UtcNow.AddHours(-24) : lastRun;
    }
}

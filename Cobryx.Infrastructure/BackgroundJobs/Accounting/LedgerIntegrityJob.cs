using Cobryx.Application.Common.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Accounting;

public class LedgerIntegrityJob(
    ILedgerIntegrityService integrityService,
    CobryxDbContext dbContext,
    ILogger<LedgerIntegrityJob> logger)
{
    private readonly ILedgerIntegrityService _integrityService = integrityService;
    private readonly CobryxDbContext _dbContext = dbContext;
    private readonly ILogger<LedgerIntegrityJob> _logger = logger;

    [Queue("ledger-integrity")]
    [AutomaticRetry(Attempts = 1)] // Small retry for transient DB issues
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Global Ledger Integrity Scan...");

        // Scan all tenants with ledger activity
        var tenantIds = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Select(a => a.TenantId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} tenants with ledger activity.", tenantIds.Count);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                _logger.LogInformation("Verifying Integrity for Tenant {TenantId}...", tenantId);
                var report = await _integrityService.VerifyJournalIntegrityAsync(tenantId, forceFullReplay: false, ct: ct);

                if (report.IsHealthy)
                {
                    _logger.LogInformation("Tenant {TenantId} Integrity: HEALTHY. Scanned: {Count}", tenantId, report.TotalEntriesScanned);
                }
                else
                {
                    _logger.LogWarning("Tenant {TenantId} Integrity: CORRUPTED! Fingerprint: {Fingerprint}", tenantId, report.JournalFingerprint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify integrity for Tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Global Ledger Integrity Scan completed.");
    }
}

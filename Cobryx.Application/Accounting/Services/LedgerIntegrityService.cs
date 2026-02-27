using System.Security.Cryptography;
using System.Text;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public class LedgerIntegrityService : ILedgerIntegrityService
{
    private readonly ICobryxDbContext _dbContext;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<LedgerIntegrityService> _logger;

    public LedgerIntegrityService(
        ICobryxDbContext dbContext,
        CobryxMetrics metrics,
        ILogger<LedgerIntegrityService> _logger)
    {
        _dbContext = dbContext;
        _metrics = metrics;
        this._logger = _logger;
    }

    public async Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting Institutional Ledger Integrity Scan for Tenant {TenantId}", tenantId);

        var details = new List<string>();
        var imbalancedCount = 0;
        var orphanCount = 0;
        var entriesScanned = 0;
        var currentFingerprint = "INITIAL_STATE";

        // 1. Transaction Balance Pass (Sum Debits == Sum Credits)
        var imbalancedTransactions = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .Include(t => t.Entries)
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (var tx in imbalancedTransactions)
        {
            var sum = tx.Entries.Sum(e => e.Debit - e.Credit);
            if (Math.Abs(sum) > 0.0001m)
            {
                imbalancedCount++;
                details.Add($"Imbalanced Transaction {tx.Id}: Net={sum}");
            }
        }

        // 2. Hash-Chain Integrity Pass (Immutability check)
        var accountIds = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var entries = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => accountIds.Contains(e.AccountId))
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToListAsync(ct);

        using (var sha256 = SHA256.Create())
        {
            foreach (var entry in entries)
            {
                entriesScanned++;
                var entryData = $"{entry.Id}|{entry.TransactionId}|{entry.AccountId}|{entry.Debit}|{entry.Credit}|{entry.CreatedAt:O}";
                var hashInput = currentFingerprint + entryData;
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                currentFingerprint = Convert.ToHexString(bytes);
            }
        }

        // 3. Trip Circuit Breaker if corruption detected
        var isHealthy = imbalancedCount == 0 && orphanCount == 0;
        var circuitBreakerTripped = false;

        if (!isHealthy)
        {
            circuitBreakerTripped = await TripCircuitBreakerAsync(tenantId, ct);
            _metrics.LedgerIntegrityFailureTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
        }

        // Record metrics
        _metrics.ReplayEntriesScannedTotal.Add(entriesScanned, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
        _metrics.ReplayDuration.Record((DateTime.UtcNow - startTime).TotalSeconds, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

        _logger.LogInformation("Integrity Scan completed. Healthy: {IsHealthy}, Fingerprint: {Fingerprint}", isHealthy, currentFingerprint);

        return new IntegrityReport(
            isHealthy,
            entriesScanned,
            imbalancedCount,
            orphanCount,
            currentFingerprint,
            details,
            circuitBreakerTripped
        );
    }

    public async Task<bool> CheckCircuitBreakersAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        return tenant?.FinancialSafeMode ?? false;
    }

    private async Task<bool> TripCircuitBreakerAsync(Guid tenantId, CancellationToken ct)
    {
        Tenant? tenant = null;

        if (_dbContext is DbContext db)
        {
            // Check if already tracked to avoid "already being tracked" errors
            tenant = db.Set<Tenant>().Local.FirstOrDefault(t => t.Id == tenantId)
                     ?? await db.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        }
        else
        {
            tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        }

        if (tenant != null && !tenant.FinancialSafeMode)
        {
            _logger.LogCritical("CRITICAL INTEGRITY FAILURE: Tripping Financial Circuit Breaker for Tenant {TenantId}", tenantId);
            tenant.ToggleFinancialSafeMode(true);
            _metrics.CircuitBreakerTrippedTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
        return false;
    }
}

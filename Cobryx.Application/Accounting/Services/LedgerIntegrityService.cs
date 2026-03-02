using System.Security.Cryptography;
using System.Text;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public class LedgerIntegrityService(
    ICobryxDbContext dbContext,
    CobryxMetrics metrics,
    ILogger<LedgerIntegrityService> logger) : ILedgerIntegrityService
{
    private readonly ICobryxDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly CobryxMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILogger<LedgerIntegrityService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, bool forceFullReplay = false, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var details = new List<string>();
        var imbalancedCount = 0;
        var orphanCount = 0;
        var currentFingerprint = "INITIAL_STATE";
        Guid lastProcessedEntryId = Guid.Empty;
        DateTime lastProcessedEntryDate = DateTime.MinValue;

        // 1. Fetch Checkpoint for Incremental Scan
        JournalCheckpoint? checkpoint = null;
        if (!forceFullReplay)
        {
            checkpoint = await _dbContext.JournalCheckpoints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

            if (checkpoint != null)
            {
                currentFingerprint = checkpoint.LastFingerprint;
                lastProcessedEntryId = checkpoint.LastProcessedEntryId;
                lastProcessedEntryDate = checkpoint.LastEntryCreatedAt;
                _logger.LogInformation("Resuming Incremental Integrity Scan from Checkpoint (EntryId: {EntryId}, Date: {Date:O})", lastProcessedEntryId, lastProcessedEntryDate);
            }
        }
        else
        {
            _logger.LogInformation("Starting FORCED Full Ledger Integrity Scan for Tenant {TenantId}", tenantId);
        }

        // 2. Transaction Balance Pass (Streaming)
        var transactionQuery = _dbContext.LedgerTransactions
            .AsNoTracking()
            .Include(t => t.Entries)
            .Where(t => t.TenantId == tenantId);

        if (checkpoint != null && !forceFullReplay)
        {
            transactionQuery = transactionQuery.Where(t => t.CreatedAt >= checkpoint.LastEntryCreatedAt);
        }

        await foreach (var tx in transactionQuery.AsAsyncEnumerable().WithCancellation(ct))
        {
            var sum = tx.Entries.Sum(e => e.Debit - e.Credit);
            if (Math.Abs(sum) > 0.0001m)
            {
                imbalancedCount++;
                details.Add($"Imbalanced Transaction {tx.Id}: Net={sum}");
            }
        }

        // 3. Hash-Chain Integrity Pass (Streaming Delta)
        var accountIds = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var entryQuery = _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => accountIds.Contains(e.AccountId));

        if (!forceFullReplay && checkpoint != null)
        {
            // Monotonic range scan using composite threshold (Date + Id tiebreaker)
            entryQuery = entryQuery.Where(e => e.CreatedAt > checkpoint.LastEntryCreatedAt || (e.CreatedAt == checkpoint.LastEntryCreatedAt && e.Id.CompareTo(checkpoint.LastProcessedEntryId) > 0));
        }

        var orderedEntryQuery = entryQuery
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id);

        int scannedInThisRun = 0;
        DateTime currentLastDate = lastProcessedEntryDate;

        await foreach (var entry in orderedEntryQuery.AsAsyncEnumerable().WithCancellation(ct))
        {
            scannedInThisRun++;
            // Optimization: Reduce string allocations by using a more direct approach if possible,
            // but keep the format identical for fingerprint stability.
            var entryData = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"{entry.Id}|{entry.TransactionId}|{entry.AccountId}|{entry.Debit}|{entry.Credit}|{entry.CreatedAt:O}");

            var hashInput = currentFingerprint + entryData;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            currentFingerprint = Convert.ToHexString(bytes);
            lastProcessedEntryId = entry.Id;
            currentLastDate = entry.CreatedAt;

            // Micro-Checkpoint every 10k items to protect against state loss during hours-long replays
            if (scannedInThisRun % 10000 == 0)
            {
                _logger.LogInformation("Streaming Micro-Checkpoint: {Count} entries sealed (Tenant: {TenantId})", scannedInThisRun, tenantId);
                await UpdateCheckpointInternalAsync(tenantId, lastProcessedEntryId, currentLastDate, currentFingerprint, (checkpoint?.EntryCount ?? 0) + scannedInThisRun, ct);
            }
        }

        var isHealthy = imbalancedCount == 0 && orphanCount == 0;
        var circuitBreakerTripped = false;

        if (!isHealthy)
        {
            circuitBreakerTripped = await TripCircuitBreakerAsync(tenantId, ct);
            _metrics.LedgerIntegrityFailureTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
        }
        else if (scannedInThisRun > 0 || (forceFullReplay && scannedInThisRun == 0))
        {
            // Final seal for this run
            await UpdateCheckpointInternalAsync(tenantId, lastProcessedEntryId, currentLastDate, currentFingerprint, (checkpoint?.EntryCount ?? 0) + scannedInThisRun, ct);
        }

        _metrics.ReplayEntriesScannedTotal.Add(scannedInThisRun, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
        _metrics.ReplayDuration.Record((DateTime.UtcNow - startTime).TotalSeconds, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

        _logger.LogInformation("Integrity Scan completed. Healthy: {IsHealthy}, Fingerprint: {Fingerprint}, Delta: {Delta}",
            isHealthy, currentFingerprint, scannedInThisRun);

        return new IntegrityReport(
            isHealthy,
            scannedInThisRun,
            imbalancedCount,
            orphanCount,
            currentFingerprint,
            details,
            circuitBreakerTripped
        );
    }

    private async Task UpdateCheckpointInternalAsync(Guid tenantId, Guid lastEntryId, DateTime lastEntryCreatedAt, string fingerprint, int cumulativeCount, CancellationToken ct)
    {
        var checkpoint = await _dbContext.JournalCheckpoints
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (checkpoint == null)
        {
            checkpoint = new JournalCheckpoint(tenantId, lastEntryId, lastEntryCreatedAt, fingerprint, cumulativeCount);
            _dbContext.JournalCheckpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.UpdateCheckpoint(lastEntryId, lastEntryCreatedAt, fingerprint, cumulativeCount);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> CheckCircuitBreakersAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        return tenant?.FinancialSafeMode ?? false;
    }

    private async Task<bool> TripCircuitBreakerAsync(Guid tenantId, CancellationToken ct)
    {
        _logger.LogWarning("Attempting to trip circuit breaker for Tenant {TenantId}", tenantId);
        Tenant? tenant = null;

        if (_dbContext == null) throw new InvalidOperationException("DbContext is null in TripCircuitBreakerAsync");

        if (_dbContext is DbContext db)
        {
            _logger.LogDebug("DbContext is standard DbContext. Checking local cache.");
            var set = db.Set<Tenant>() ?? throw new InvalidOperationException("DbSet<Tenant> is null");

            tenant = set.Local.FirstOrDefault(t => t.Id == tenantId)
                     ?? await set.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        }
        else
        {
            _logger.LogDebug("DbContext is not standard DbContext. Using interface Tenants property.");
            tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        }

        if (tenant != null && !tenant.FinancialSafeMode)
        {
            _logger.LogCritical("CRITICAL INTEGRITY FAILURE: Tripping Financial Circuit Breaker for Tenant {TenantId}", tenantId);
            tenant.ToggleFinancialSafeMode(true);

            if (_metrics?.CircuitBreakerTrippedTotal == null)
                _logger.LogWarning("Metrics or CircuitBreakerTrippedTotal is null. Skipping metric recording.");
            else
                _metrics.CircuitBreakerTrippedTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        _logger.LogWarning("Could not trip circuit breaker. Tenant found: {Found}", tenant != null);
        return false;
    }
}

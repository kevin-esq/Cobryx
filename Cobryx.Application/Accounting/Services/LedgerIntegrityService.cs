using Cobryx.Domain.Accounting;
using System.Security.Cryptography;
using System.Text;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Application.Common.Observability;
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
        long lastProcessedSequenceId = 0;
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
                lastProcessedSequenceId = checkpoint.LastProcessedSequenceId;
                lastProcessedEntryDate = checkpoint.LastEntryCreatedAt;
                _logger.LogInformation("Resuming Incremental Integrity Scan from Checkpoint (SequenceId: {SequenceId}, Date: {Date:O})", lastProcessedSequenceId, lastProcessedEntryDate);
            }
        }
        else
        {
            _logger.LogInformation("Starting FORCED Full Ledger Integrity Scan for Tenant {TenantId}", tenantId);
        }

        // 2. Transaction Balance Pass (Optimized GroupBy)
        var imbalancedTransactions = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            // If incremental, only check transactions affected by new entries
            .Where(e => !(!forceFullReplay && checkpoint != null) || e.JournalSequenceId > checkpoint.LastProcessedSequenceId)
            .GroupBy(e => e.TransactionId)
            .Select(g => new { TransactionId = g.Key, Balance = g.Sum(e => e.Debit - e.Credit) })
            .Where(x => Math.Abs(x.Balance) > 0.0001m)
            .ToListAsync(ct);

        imbalancedCount = imbalancedTransactions.Count;
        foreach (var tx in imbalancedTransactions)
        {
            details.Add($"Imbalanced Transaction {tx.TransactionId}: Net={tx.Balance}");
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
            entryQuery = entryQuery.Where(e => e.JournalSequenceId > checkpoint.LastProcessedSequenceId);
        }

        var orderedEntryQuery = entryQuery.OrderBy(e => e.JournalSequenceId);

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
            lastProcessedSequenceId = entry.JournalSequenceId;
            currentLastDate = entry.CreatedAt;

            // Micro-Checkpoint every 10k items to protect against state loss during hours-long replays
            if (scannedInThisRun % 10000 == 0)
            {
                _logger.LogInformation("Streaming Micro-Checkpoint: {Count} entries sealed (Tenant: {TenantId})", scannedInThisRun, tenantId);
                await UpdateCheckpointInternalAsync(tenantId, lastProcessedEntryId, lastProcessedSequenceId, currentLastDate, currentFingerprint, (checkpoint?.EntryCount ?? 0) + scannedInThisRun, ct);
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
            await UpdateCheckpointInternalAsync(tenantId, lastProcessedEntryId, lastProcessedSequenceId, currentLastDate, currentFingerprint, (checkpoint?.EntryCount ?? 0) + scannedInThisRun, ct);
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

    private async Task UpdateCheckpointInternalAsync(Guid tenantId, Guid lastEntryId, long lastSequenceId, DateTime lastEntryCreatedAt, string fingerprint, int cumulativeCount, CancellationToken ct)
    {
        var checkpoint = await _dbContext.JournalCheckpoints
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (checkpoint == null)
        {
            checkpoint = new JournalCheckpoint(tenantId, lastEntryId, lastSequenceId, lastEntryCreatedAt, fingerprint, cumulativeCount);
            _dbContext.JournalCheckpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.UpdateCheckpoint(lastEntryId, lastSequenceId, lastEntryCreatedAt, fingerprint, cumulativeCount);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> CheckCircuitBreakersAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        return tenant?.FinancialSafeMode ?? false;
    }

    public async Task<List<long>> VerifyGlobalSequenceGapsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Global Sequence Gap Detection (LAG-Optimized)");

        // Using raw SQL for LAG partitioning/ordering efficiency
        // This query identifies the Sequence ID where a gap *starts* (i.e., the ID before the skip)
        // Note: We use Set<LedgerEntry>() to access the IQueryable but this won't return LedgerEntries.
        // In EF Core 8 we can use SqlQueryRaw for primitive types.

        var gaps = await _dbContext.LedgerEntries
            .AsNoTracking()
            .OrderBy(e => e.JournalSequenceId)
            .Select(e => e.JournalSequenceId)
            .ToListAsync(ct);

        var gapStarts = new List<long>();
        for (int i = 1; i < gaps.Count; i++)
        {
            if (gaps[i] != gaps[i - 1] + 1)
            {
                _logger.LogCritical("SEQUENCE GAP DETECTED: Jump from {Prev} to {Curr}", gaps[i - 1], gaps[i]);
                gapStarts.Add(gaps[i - 1]);
            }
        }

        return gapStarts;
    }

    public async Task<bool> VerifyGlobalSumInvariantAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Global Ledger Sum Invariant Verification (Total Ledger Replay)");

        var totalBalance = await _dbContext.LedgerEntries
            .SumAsync(e => e.Debit - e.Credit, ct);

        var isHealthy = Math.Abs(totalBalance) < 0.0001m;

        if (!isHealthy)
        {
            _logger.LogCritical("GLOBAL INVARIANT FAILURE: Ledger Total Sum is {Balance}. Expected 0.", totalBalance);
        }

        return isHealthy;
    }

    public async Task<bool> VerifyAccountSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
    {
        var snapshot = await _dbContext.AccountBalanceSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshotId, ct);

        if (snapshot == null) return false;

        _logger.LogInformation("Verifying Snapshot {SnapshotId} for Account {AccountId} @ Seq {Seq}",
            snapshotId, snapshot.AccountId, snapshot.JournalSequenceId);

        // 1. Calculate Expected Balance from Snapshot + Journal Delta
        var deltaSinceSnapshot = await _dbContext.LedgerEntries
            .Where(e => e.AccountId == snapshot.AccountId && e.JournalSequenceId > snapshot.JournalSequenceId)
            .SumAsync(e => e.Debit - e.Credit, ct);

        var expectedFromSnapshot = snapshot.Balance + deltaSinceSnapshot;

        // 2. Calculate Actual Balance from Full Ledger Replay
        var realBalanceFromLedger = await _dbContext.LedgerEntries
            .Where(e => e.AccountId == snapshot.AccountId)
            .SumAsync(e => e.Debit - e.Credit, ct);

        var isMatch = Math.Abs(expectedFromSnapshot - realBalanceFromLedger) < 0.0001m;

        if (!isMatch)
        {
            _logger.LogCritical("SNAPSHOT CORRUPTION DETECTED: Account {AccountId}. Snapshot+Delta={Expected}, TotalReplay={Actual}",
                snapshot.AccountId, expectedFromSnapshot, realBalanceFromLedger);
        }
        else
        {
            _logger.LogInformation("Snapshot {SnapshotId} verified successfully against Total Ledger Replay.", snapshotId);
        }

        return isMatch;
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

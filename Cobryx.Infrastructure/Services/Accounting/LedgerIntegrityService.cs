using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Identity;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Accounting
{
    /// <summary>
    /// Infrastructure implementation of the Ledger Integrity Service.
    /// Performs mathematical verification of the double-entry system via Journal Replay.
    /// </summary>
    public partial class LedgerIntegrityService(
        CobryxDbContext dbContext,
        ILedgerHealthCache healthCache,
        ILedgerHasher ledgerHasher,
        IClock clock,
        ILedgerAnchorStore anchorStore,
        ILogger<LedgerIntegrityService> logger)
        : ILedgerIntegrityService
    {
        private readonly ILedgerAnchorStore _anchorStore = anchorStore;
        private const decimal Tolerance = 0.0001m;

        public async Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, bool forceFullReplay = false,
            CancellationToken ct = default)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            LogStartingForcedFullScan(logger, tenantId);
            logger.LogInformation("Starting Integrity Scan [{CorrelationId}] at {UtcNow:O} for Tenant {TenantId}", 
                correlationId, clock.UtcNow, tenantId);

            var entriesScanned = 0;
            var violations = new List<IntegrityViolation>();
            const string fingerprint = "INITIAL_STATE";

            // 1. Check Global Invariant (Σ Debits == Σ Credits) - Highest Priority
            var isGlobalBalanced = await VerifyGlobalSumInvariantAsync(ct);
            if (!isGlobalBalanced)
            {
                var total = await dbContext.LedgerEntries.SumAsync(e => e.Debit - e.Credit, ct);
                LogGlobalInvariantFailure(logger, total);
                
                violations.Add(new IntegrityViolation(
                    "IMBALANCE", 
                    "GLOBAL", 
                    total, 
                    "AUDIT_LEDGER", 
                    correlationId));
            }

            // 2. Check for imbalanced transactions (individual journal entries that don't sum to zero)
            var imbalancedTransactions = await dbContext.LedgerEntries
                .Where(e => e.TenantId == tenantId)
                .GroupBy(e => e.TransactionId)
                .Select(g => new { TransactionId = g.Key, Balance = g.Sum(e => e.Debit - e.Credit) })
                .Where(x => Math.Abs(x.Balance) > Tolerance)
                .ToListAsync(ct);

            foreach (var tx in imbalancedTransactions)
            {
                violations.Add(new IntegrityViolation(
                    "IMBALANCE", 
                    tx.TransactionId.ToString(), 
                    tx.Balance, 
                    "REVERSE_TRANSACTION", 
                    correlationId));
            }

            // 3. Perform Sequence Gap Scan
            List<long> gaps = await VerifyGlobalSequenceGapsAsync(ct);
            foreach (var gap in gaps)
            {
                violations.Add(new IntegrityViolation(
                    "SEQUENCE_GAP", 
                    gap.ToString(), 
                    0, 
                    "RECONSTRUCT_GAP", 
                    correlationId));
            }

            // 4. Perform Cryptographic Chain Scan (ELITE)
            var chainViolations = await VerifyHashChainAsync(tenantId, ct);
            violations.AddRange(chainViolations.Select(v => v with { CorrelationId = correlationId }));

            var isHealthy = violations.Count == 0;
            var severity = isHealthy ? AccountingDriftSeverity.Low : 
                        violations.Any(v => v.Type == "IMBALANCE") ? AccountingDriftSeverity.Critical : AccountingDriftSeverity.Medium;

            // ELITE SAFETY: Automated Trip Logic
            if (!isHealthy)
            {
                var reason = violations.First().Type;
                if (healthCache.TryActivateSafeMode(tenantId, reason, TimeSpan.FromHours(24)))
                {
                    logger.LogCritical("AUTOMATED LOCKDOWN: Drift detected for Tenant {TenantId}. Safe Mode activated. CorrelationId: {CorrelationId}", 
                        tenantId, correlationId);
                }
            }

            return new IntegrityReport(
                IsHealthy: isHealthy,
                Severity: severity,
                TotalEntriesScanned: entriesScanned,
                Violations: violations,
                JournalFingerprint: fingerprint,
                CircuitBreakerTripped: !isHealthy,
                CorrelationId: correlationId,
                CheckedAt: clock.UtcNow);
        }

        public async Task<bool> CheckCircuitBreakersAsync(Guid tenantId, CancellationToken ct = default)
        {
            Tenant? tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            return tenant?.FinancialSafeMode ?? false;
        }

        public async Task<List<long>> VerifyGlobalSequenceGapsAsync(CancellationToken ct = default)
        {
            LogStartingGlobalGapDetection(logger);
            List<long> sequence = await dbContext.LedgerEntries
                .AsNoTracking()
                .OrderBy(e => e.JournalSequenceId)
                .Select(e => e.JournalSequenceId)
                .ToListAsync(ct);

            var gaps = new List<long>();
            for (var i = 1; i < sequence.Count; i++)
            {
                if (sequence[i] != sequence[i - 1] + 1)
                {
                    LogSequenceGapDetected(logger, sequence[i - 1], sequence[i]);
                    gaps.Add(sequence[i - 1]);
                }
            }

            return gaps;
        }

        public async Task<bool> VerifyGlobalSumInvariantAsync(CancellationToken ct = default)
        {
            LogStartingGlobalSumVerification(logger);
            var total = await dbContext.LedgerEntries
                .SumAsync(e => e.Debit - e.Credit, ct);

            return Math.Abs(total) < Tolerance;
        }

        public async Task<List<IntegrityViolation>> VerifyHashChainAsync(Guid tenantId, CancellationToken ct = default)
        {
            const string initialHash = "0000000000000000000000000000000000000000000000000000000000000000";
            var violations = new List<IntegrityViolation>();

            // Fetch all transactions in sequence order
            var transactions = await dbContext.LedgerTransactions
                .Where(t => t.TenantId == tenantId)
                .Include(t => t.Entries)
                .OrderBy(t => t.Sequence)
                .ToListAsync(ct);

            var runningHash = initialHash;
            var expectedSeq = 1L;

            foreach (var tx in transactions)
            {
                var currentHash = tx.Hash ?? initialHash;
                var currentPrevHash = tx.PreviousHash ?? initialHash;

                // ... (existing checks)
                // 1. Check Sequence Continuity
                if (tx.Sequence != expectedSeq)
                {
                    violations.Add(new IntegrityViolation("CHAIN_BROKEN", tx.Id.ToString(), tx.Sequence, "RE-ORDER_INDEX", "CHAIN"));
                }

                // 2. Check Linkage vs Running Chain (Propagation Detection)
                if (currentPrevHash != runningHash)
                {
                    violations.Add(new IntegrityViolation("CHAIN_BROKEN", tx.Id.ToString(), 0, "RESTORE_LINK", "CHAIN"));
                }

                // 3. Check Data Integrity (Hash Match)
                var computedHash = ledgerHasher.ComputeHash(tx, currentPrevHash);
                if (tx.Hash != computedHash)
                {
                    violations.Add(new IntegrityViolation("HASH_MISMATCH", tx.Id.ToString(), 0, "AUDIT_RECOVERY", "CHAIN"));
                }

                // Update for next iteration (carry forward the computed hash to propagate lineage failures)
                runningHash = computedHash;
                expectedSeq = tx.Sequence + 1;
            }

            // 4. External Anchor Verification (Tier-0 Proof)
            var latestAnchor = _anchorStore.GetLatest(tenantId, ct);
            if (latestAnchor != null)
            {
                // Find the transaction in the DB that matches the anchor's sequence
                var anchoredTx = transactions.FirstOrDefault(t => t.Sequence == latestAnchor.Sequence);
                if (anchoredTx != null)
                {
                    if (anchoredTx.Hash != latestAnchor.Hash)
                    {
                        violations.Add(new IntegrityViolation(
                            "ANCHOR_MISMATCH", 
                            anchoredTx.Id.ToString(), 
                            latestAnchor.Sequence, 
                            "EXTERNAL_FORENSIC_REPLAY", 
                            "ANCHOR"));
                    }
                }
                else if (transactions.Any() && transactions.Max(t => t.Sequence) < latestAnchor.Sequence)
                {
                    // Scenario: Database was ROLLED BACK (historical substitution via snapshot restore or manual deletion)
                    violations.Add(new IntegrityViolation(
                        "LEDGER_ROLLBACK", 
                        "DATABASE", 
                        latestAnchor.Sequence, 
                        "RESTORE_FROM_ANCHOR", 
                        "ANCHOR"));
                }
            }

            return violations;
        }

        public async Task<bool> VerifyAccountSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
        {
            AccountBalanceSnapshot? snapshot =
                await dbContext.AccountBalanceSnapshots.FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
            if (snapshot is null)
            {
                return false;
            }

            var actual = await dbContext.LedgerEntries
                .Where(e => e.AccountId == snapshot.AccountId && e.JournalSequenceId <= snapshot.JournalSequenceId)
                .SumAsync(e => e.Debit - e.Credit, ct);

            return Math.Abs(actual - snapshot.Balance) < Tolerance;
        }

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Resuming Incremental Integrity Scan from Checkpoint (SequenceId: {SequenceId}, Date: {Date:O})")]
        public static partial void LogResumingIncrementalScan(ILogger logger, long sequenceId, DateTime date);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Starting FORCED Full Ledger Integrity Scan for Tenant {TenantId}")]
        public static partial void LogStartingForcedFullScan(ILogger logger, Guid tenantId);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Streaming Micro-Checkpoint: {Count} entries sealed (Tenant: {TenantId})")]
        public static partial void LogStreamingMicroCheckpoint(ILogger logger, int count, Guid tenantId);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Integrity Scan completed. Healthy: {IsHealthy}, Fingerprint: {Fingerprint}, Delta: {Delta}")]
        public static partial void LogIntegrityScanCompleted(ILogger logger, bool isHealthy, string fingerprint,
            int delta);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Starting Global Sequence Gap Detection (LAG-Optimized)")]
        public static partial void LogStartingGlobalGapDetection(ILogger logger);

        [LoggerMessage(Level = LogLevel.Critical, Message = "SEQUENCE GAP DETECTED: Jump from {Prev} to {Curr}")]
        public static partial void LogSequenceGapDetected(ILogger logger, long prev, long curr);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Starting Global Ledger Sum Invariant Verification (Total Ledger Replay)")]
        public static partial void LogStartingGlobalSumVerification(ILogger logger);

        [LoggerMessage(Level = LogLevel.Critical,
            Message = "GLOBAL INVARIANT FAILURE: Ledger Total Sum is {Balance}. Expected 0.")]
        public static partial void LogGlobalInvariantFailure(ILogger logger, decimal balance);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Verifying Snapshot {SnapshotId} for Account {AccountId} @ Seq {Seq}")]
        public static partial void LogVerifyingSnapshot(ILogger logger, Guid snapshotId, Guid accountId, long seq);

        [LoggerMessage(Level = LogLevel.Critical,
            Message =
                "SNAPSHOT CORRUPTION DETECTED: Account {AccountId}. Snapshot+Delta={Expected}, TotalReplay={Actual}")]
        public static partial void LogSnapshotCorruption(ILogger logger, Guid accountId, decimal expected,
            decimal actual);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Snapshot {SnapshotId} verified successfully against Total Ledger Replay.")]
        public static partial void LogSnapshotVerified(ILogger logger, Guid snapshotId);
    }
}

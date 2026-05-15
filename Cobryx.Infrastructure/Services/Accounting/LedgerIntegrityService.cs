using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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

        private bool UseClientSideDecimalAggregate =>
            dbContext.Database.ProviderName is string p &&
            (p.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ||
             p.Contains("InMemory", StringComparison.OrdinalIgnoreCase));

        private async Task<decimal> SumDebitMinusCreditAsync(IQueryable<LedgerEntry> entries, CancellationToken ct)
        {
            if (UseClientSideDecimalAggregate)
            {
                List<decimal> deltas = await entries.Select(static e => e.Debit - e.Credit).ToListAsync(ct);
                decimal total = 0;
                foreach (var d in deltas)
                {
                    total += d;
                }

                return total;
            }

            return await entries.SumAsync(static e => e.Debit - e.Credit, ct);
        }

        /// <remarks>
        /// SQLite cannot translate SUM over decimal aggregates; grouping by transaction is aggregated in-memory.
        /// </remarks>
        private async Task<List<(Guid TransactionId, decimal Balance)>> GetImbalancedJournalTransactionsAsync(Guid tenantId,
            CancellationToken ct)
        {
            var rows = await dbContext.LedgerEntries.AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .Select(e => new { e.TransactionId, Delta = e.Debit - e.Credit })
                .ToListAsync(ct);

            return rows.GroupBy(static r => r.TransactionId)
                .Select(g => (g.Key, Balance: g.Sum(static r => r.Delta)))
                .Where(t => Math.Abs(t.Balance) > Tolerance)
                .Select(static t => (t.Key, t.Balance))
                .ToList();
        }

        private async Task<string> ComputeJournalFingerprintAsync(Guid tenantId, bool forceFullReplay,
            CancellationToken ct)
        {
            IQueryable<LedgerEntry> scoped =
                dbContext.LedgerEntries.AsNoTracking().Where(e => e.TenantId == tenantId);

            var count = await scoped.CountAsync(ct);
            decimal sum = await SumDebitMinusCreditAsync(scoped, ct);
            long maxJournalSeq =
                await scoped.Select(static e => (long)e.JournalSequenceId).DefaultIfEmpty().MaxAsync(ct);

            var summary =
                $"{tenantId:N}|{count}|{sum.ToString(CultureInfo.InvariantCulture)}|{maxJournalSeq}";

            if (!forceFullReplay)
            {
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(summary)));
            }

            var lineRows = await scoped
                .OrderBy(static e => e.JournalSequenceId).ThenBy(static e => e.Id)
                .Select(e => new { e.TransactionId, e.AccountId, e.Debit, e.Credit, e.JournalSequenceId })
                .ToListAsync(ct);

            var sb = new StringBuilder(capacity: 256 + lineRows.Count * 96)
                .Append(summary)
                .Append("|FULL|");

            foreach (var r in lineRows)
            {
                _ = sb.Append(r.TransactionId.ToString("N")).Append('|')
                    .Append(r.AccountId.ToString("N")).Append('|')
                    .Append(r.Debit.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(r.Credit.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(r.JournalSequenceId.ToString(NumberFormatInfo.InvariantInfo))
                    .Append(';');
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        public async Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, bool forceFullReplay = false,
            CancellationToken ct = default)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            LogStartingForcedFullScan(logger, tenantId);
            logger.LogInformation("Starting Integrity Scan [{CorrelationId}] at {UtcNow:O} for Tenant {TenantId}", 
                correlationId, clock.UtcNow, tenantId);

            var entriesScanned = await dbContext.LedgerEntries.CountAsync(e => e.TenantId == tenantId, ct);
            var violations = new List<IntegrityViolation>();

            // 1. Check Global Invariant (Σ Debits == Σ Credits) - Highest Priority
            decimal netGlobal =
                await SumDebitMinusCreditAsync(dbContext.LedgerEntries.AsNoTracking(), ct);
            if (Math.Abs(netGlobal) >= Tolerance)
            {
                LogGlobalInvariantFailure(logger, netGlobal);
                violations.Add(new IntegrityViolation(
                    "IMBALANCE", 
                    "GLOBAL", 
                    netGlobal, 
                    "AUDIT_LEDGER", 
                    correlationId));
            }

            // 2. Check for imbalanced transactions (individual journal entries that don't sum to zero)
            foreach ((Guid transactionId, decimal balance) in await GetImbalancedJournalTransactionsAsync(tenantId, ct))
            {
                violations.Add(new IntegrityViolation(
                    "IMBALANCE", 
                    transactionId.ToString(), 
                    balance, 
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
                var reason = violations[0].Type;
                if (healthCache.TryActivateSafeMode(tenantId, reason, TimeSpan.FromHours(24)))
                {
                    logger.LogCritical("AUTOMATED LOCKDOWN: Drift detected for Tenant {TenantId}. Safe Mode activated. CorrelationId: {CorrelationId}", 
                        tenantId, correlationId);
                }
            }

            var journalFingerprint = await ComputeJournalFingerprintAsync(tenantId, forceFullReplay, ct);

            return new IntegrityReport(
                IsHealthy: isHealthy,
                Severity: severity,
                TotalEntriesScanned: entriesScanned,
                Violations: violations,
                JournalFingerprint: journalFingerprint,
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
            decimal total =
                await SumDebitMinusCreditAsync(dbContext.LedgerEntries.AsNoTracking(), ct);

            return Math.Abs(total) < Tolerance;
        }

        public async Task<List<IntegrityViolation>> VerifyHashChainAsync(Guid tenantId, CancellationToken ct = default)
        {
            const string initialHash = "0000000000000000000000000000000000000000000000000000000000000000";
            var violations = new List<IntegrityViolation>();

            // Only sealed transactions participate in the cryptographically linked chain.
            // Ungrouped migrations/tests may still have rows without Hash/Sequence — skip them here.
            var transactions = await dbContext.LedgerTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.Hash != null)
                .Include(t => t.Entries)
                .OrderBy(t => t.Sequence)
                .ToListAsync(ct);

            var runningHash = initialHash;
            long expectedSeq = transactions.Count > 0 ? transactions[0].Sequence : 1L;

            foreach (var tx in transactions)
            {
                var currentHash = tx.Hash!; // filtered non-null
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
                if (!string.Equals(tx.Hash, computedHash, StringComparison.Ordinal))
                {
                    violations.Add(new IntegrityViolation("HASH_MISMATCH", tx.Id.ToString(), 0, "AUDIT_RECOVERY", "CHAIN"));
                }

                // Update for next iteration (carry forward the computed hash to propagate lineage failures)
                runningHash = computedHash ?? currentHash;
                expectedSeq = tx.Sequence + 1;
            }

            // 4. External Anchor Verification (Tier-0 Proof)
            var latestAnchor = _anchorStore.GetLatest(tenantId, ct);
            if (latestAnchor != null)
            {
                LedgerTransaction? anchoredTx = transactions.FirstOrDefault(t => t.Sequence == latestAnchor.Sequence);
                if (anchoredTx != null)
                {
                    if (!string.Equals(anchoredTx.Hash, latestAnchor.Hash, StringComparison.Ordinal))
                    {
                        violations.Add(new IntegrityViolation(
                            "ANCHOR_MISMATCH",
                            anchoredTx.Id.ToString(),
                            latestAnchor.Sequence,
                            "EXTERNAL_FORENSIC_REPLAY",
                            "ANCHOR"));
                    }
                }
                else
                {
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

            IQueryable<LedgerEntry> scope = dbContext.LedgerEntries.AsNoTracking()
                .Where(e => e.AccountId == snapshot.AccountId && e.JournalSequenceId <= snapshot.JournalSequenceId);

            decimal actual = await SumDebitMinusCreditAsync(scope, ct);

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

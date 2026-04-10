using System.Security.Cryptography;
using System.Text;

using Cobryx.Application.Accounting.Models;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services
{
    public partial class LedgerIntegrityService(
        ICobryxDbContext dbContext,
        CobryxMetrics metrics,
        IClock clock,
        ILogger<LedgerIntegrityService> logger) : ILedgerIntegrityService
    {
        private const decimal Tolerance = 0.0001m;
        private const int MicroCheckpointBatchSize = 10_000;

        public async Task<IntegrityReport> VerifyJournalIntegrityAsync(
            Guid tenantId,
            bool forceFullReplay = false,
            CancellationToken ct = default)
        {
            DateTime start = clock.UtcNow;

            (JournalCheckpoint? Checkpoint, string Fingerprint, Guid LastEntryId, long LastSeq, DateTime LastDate)
                state = await InitializeStateAsync(tenantId, forceFullReplay, ct);

            List<LedgerEntryTransactionBalance> imbalanced =
                await GetImbalancedTransactionsAsync(tenantId, state, forceFullReplay, ct);

            var details = imbalanced
                .Select(tx => $"Imbalanced Transaction {tx.TransactionId}: Net={tx.Balance}")
                .ToList();

            (var scanned, var fingerprint, Guid lastEntryId, var lastSeq, DateTime lastDate) =
                await StreamAndHashEntriesAsync(tenantId, state, forceFullReplay, ct);

            var isHealthy = imbalanced.Count == 0;
            var circuitBreaker = false;

            if (!isHealthy)
            {
                circuitBreaker = await TripCircuitBreakerAsync(tenantId, ct);
                metrics.LedgerIntegrityFailureTotal.Add(1,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
            }
            else if (scanned > 0 || (forceFullReplay && scanned == 0))
            {
                await UpdateCheckpointInternalAsync(
                    tenantId,
                    lastEntryId,
                    lastSeq,
                    lastDate,
                    fingerprint,
                    (state.Checkpoint?.EntryCount ?? 0) + scanned,
                    ct);
            }

            metrics.ReplayEntriesScannedTotal.Add(scanned,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            metrics.ReplayDuration.Record(
                (clock.UtcNow - start).TotalSeconds,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            LogIntegrityScanCompleted(logger, isHealthy, fingerprint, scanned);

            return new IntegrityReport(
                isHealthy,
                scanned,
                imbalanced.Count,
                0,
                fingerprint,
                details,
                circuitBreaker);
        }

        private async Task<(JournalCheckpoint? Checkpoint, string Fingerprint, Guid LastEntryId, long LastSeq, DateTime
                LastDate)>
            InitializeStateAsync(Guid tenantId, bool forceFullReplay, CancellationToken ct)
        {
            if (forceFullReplay)
            {
                LogStartingForcedFullScan(logger, tenantId);
                return (null, "INITIAL_STATE", Guid.Empty, 0, DateTime.MinValue);
            }

            JournalCheckpoint? checkpoint = await dbContext.JournalCheckpoints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

            if (checkpoint is null)
            {
                return (null, "INITIAL_STATE", Guid.Empty, 0, DateTime.MinValue);
            }

            LogResumingIncrementalScan(logger, checkpoint.LastProcessedSequenceId, checkpoint.LastEntryCreatedAt);

            return (
                checkpoint,
                checkpoint.LastFingerprint,
                checkpoint.LastProcessedEntryId,
                checkpoint.LastProcessedSequenceId,
                checkpoint.LastEntryCreatedAt);
        }

        private async Task<List<LedgerEntryTransactionBalance>> GetImbalancedTransactionsAsync(
            Guid tenantId,
            (JournalCheckpoint? Checkpoint, string Fingerprint, Guid LastEntryId, long LastSeq, DateTime LastDate)
                state,
            bool forceFullReplay,
            CancellationToken ct)
        {
            return await dbContext.LedgerEntries
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .Where(e => forceFullReplay || state.Checkpoint == null ||
                            e.JournalSequenceId > state.Checkpoint.LastProcessedSequenceId)
                .GroupBy(static e => e.TransactionId)
                .Select(static g => new LedgerEntryTransactionBalance(g.Key, g.Sum(static e => e.Debit - e.Credit)))
                .Where(static x => Math.Abs(x.Balance) > Tolerance)
                .ToListAsync(ct);
        }

        private async Task<(int Scanned, string Fingerprint, Guid LastEntryId, long LastSeq, DateTime LastDate)>
            StreamAndHashEntriesAsync(
                Guid tenantId,
                (JournalCheckpoint? Checkpoint, string Fingerprint, Guid LastEntryId, long LastSeq, DateTime LastDate)
                    state,
                bool forceFullReplay,
                CancellationToken ct)
        {
            var fingerprint = state.Fingerprint;
            Guid lastEntryId = state.LastEntryId;
            var lastSeq = state.LastSeq;
            DateTime lastDate = state.LastDate;

            IQueryable<LedgerEntry> query = dbContext.LedgerEntries
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId);

            if (!forceFullReplay && state.Checkpoint != null)
            {
                var seq = state.Checkpoint.LastProcessedSequenceId;
                query = query.Where(e => e.JournalSequenceId > seq);
            }

            IOrderedQueryable<LedgerEntry> ordered = query.OrderBy(static e => e.JournalSequenceId);

            var scanned = 0;

            await foreach (LedgerEntry entry in ordered.AsAsyncEnumerable().WithCancellation(ct))
            {
                scanned++;

                var data = string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{entry.Id}|{entry.TransactionId}|{entry.AccountId}|{entry.Debit}|{entry.Credit}|{entry.CreatedAt:O}");

                var hashInput = fingerprint + data;
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));

                fingerprint = Convert.ToHexString(bytes);
                lastEntryId = entry.Id;
                lastSeq = entry.JournalSequenceId;
                lastDate = entry.CreatedAt;

                if (scanned % MicroCheckpointBatchSize == 0)
                {
                    LogStreamingMicroCheckpoint(logger, scanned, tenantId);

                    await UpdateCheckpointInternalAsync(
                        tenantId,
                        lastEntryId,
                        lastSeq,
                        lastDate,
                        fingerprint,
                        (state.Checkpoint?.EntryCount ?? 0) + scanned,
                        ct);
                }
            }

            return (scanned, fingerprint, lastEntryId, lastSeq, lastDate);
        }

        private async Task UpdateCheckpointInternalAsync(
            Guid tenantId,
            Guid lastEntryId,
            long lastSequenceId,
            DateTime lastEntryCreatedAt,
            string fingerprint,
            int cumulativeCount,
            CancellationToken ct)
        {
            JournalCheckpoint? checkpoint = await dbContext.JournalCheckpoints
                .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

            if (checkpoint is null)
            {
                checkpoint = new JournalCheckpoint(
                    tenantId,
                    lastEntryId,
                    lastSequenceId,
                    lastEntryCreatedAt,
                    fingerprint,
                    cumulativeCount);

                _ = dbContext.JournalCheckpoints.Add(checkpoint);
            }
            else
            {
                checkpoint.UpdateCheckpoint(
                    lastEntryId,
                    lastSequenceId,
                    lastEntryCreatedAt,
                    fingerprint,
                    cumulativeCount);
            }

            _ = await dbContext.SaveChangesAsync(ct);
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
                .OrderBy(static e => e.JournalSequenceId)
                .Select(static e => e.JournalSequenceId)
                .ToListAsync(ct);

            var gaps = new List<long>();

            for (var i = 1; i < sequence.Count; i++)
            {
                if (sequence[i] == sequence[i - 1] + 1)
                {
                    continue;
                }

                LogSequenceGapDetected(logger, sequence[i - 1], sequence[i]);
                gaps.Add(sequence[i - 1]);
            }

            return gaps;
        }

        public async Task<bool> VerifyGlobalSumInvariantAsync(CancellationToken ct = default)
        {
            LogStartingGlobalSumVerification(logger);

            var total = await dbContext.LedgerEntries
                .SumAsync(static e => e.Debit - e.Credit, ct);

            var healthy = Math.Abs(total) < Tolerance;

            if (!healthy)
            {
                LogGlobalInvariantFailure(logger, total);
            }

            return healthy;
        }

        public async Task<bool> VerifyAccountSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
        {
            AccountBalanceSnapshot? snapshot = await dbContext.AccountBalanceSnapshots
                .FirstOrDefaultAsync(s => s.Id == snapshotId, ct);

            if (snapshot is null)
            {
                return false;
            }

            LogVerifyingSnapshot(logger, snapshotId, snapshot.AccountId, snapshot.JournalSequenceId);

            var delta = await dbContext.LedgerEntries
                .Where(e => e.AccountId == snapshot.AccountId && e.JournalSequenceId > snapshot.JournalSequenceId)
                .SumAsync(static e => e.Debit - e.Credit, ct);

            var expected = snapshot.Balance + delta;

            var actual = await dbContext.LedgerEntries
                .Where(e => e.AccountId == snapshot.AccountId)
                .SumAsync(static e => e.Debit - e.Credit, ct);

            var match = Math.Abs(expected - actual) < Tolerance;

            if (!match)
            {
                LogSnapshotCorruption(logger, snapshot.AccountId, expected, actual);
            }
            else
            {
                LogSnapshotVerified(logger, snapshotId);
            }

            return match;
        }

        private async Task<bool> TripCircuitBreakerAsync(Guid tenantId, CancellationToken ct)
        {
            LogAttemptingTripCircuitBreaker(logger, tenantId);

            Tenant? tenant;

            if (dbContext is DbContext db)
            {
                LogDbContextStandard(logger);

                DbSet<Tenant> set = db.Set<Tenant>() ?? throw new InvalidOperationException("DbSet<Tenant> is null");

                tenant = set.Local.FirstOrDefault(t => t.Id == tenantId)
                         ?? await set.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            }
            else
            {
                LogDbContextNotStandard(logger);
                tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            }

            if (tenant is not null && !tenant.FinancialSafeMode)
            {
                LogCriticalIntegrityFailure(logger, tenantId);

                tenant.ToggleFinancialSafeMode(true);

                metrics.CircuitBreakerTrippedTotal.Add(1,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

                _ = await dbContext.SaveChangesAsync(ct);

                return true;
            }

            LogTripCircuitBreakerFailed(logger, tenant is not null);
            return false;
        }
    }
}

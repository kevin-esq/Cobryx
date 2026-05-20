using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Jobs
{
    /// <summary>
    /// Background job to materialize account balance snapshots at specific sequence points.
    /// Ensures verifiability and speeds up balance calculations.
    /// </summary>
    public class LedgerSnapshotJob(
        ICobryxDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<LedgerSnapshotJob> logger)
    {
        private const int EntriesPerSnapshotLimit = 5000;

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            // 1. Find accounts that need a new snapshot (e.g. they have many entries since the last snapshot)
            var accounts = await dbContext.LedgerAccounts.AsNoTracking().ToListAsync(ct);

            foreach (var account in accounts)
            {
                var lastSnapshot = await dbContext.AccountBalanceSnapshots
                    .Where(s => s.AccountId == account.Id)
                    .OrderByDescending(s => s.JournalSequenceId)
                    .FirstOrDefaultAsync(ct);

                var lastSequenceId = lastSnapshot?.JournalSequenceId ?? 0;

                // 2. Count entries since last snapshot AND check time elapsed
                var entriesSinceCount = await dbContext.LedgerEntries
                    .Where(e => e.AccountId == account.Id && e.JournalSequenceId > lastSequenceId)
                    .CountAsync(ct);

                var timeSinceLastSnapshot = lastSnapshot != null
                    ? DateTime.UtcNow - lastSnapshot.CreatedAt
                    : TimeSpan.MaxValue;

                // Dual Trigger: N entries OR X time (e.g. 1 hour)
                if (entriesSinceCount < EntriesPerSnapshotLimit && timeSinceLastSnapshot < TimeSpan.FromHours(1))
                {
                    continue;
                }

                {
                    logger.LogInformation(
                        "Creating Ledger Snapshot for Account {AccountId} ({AccountName}). Reason: {Reason}",
                        account.Id, account.Name,
                        entriesSinceCount >= EntriesPerSnapshotLimit ? "Sequence Threshold" : "Temporal Threshold");

                    // 3. Calculate current balance at the latest sequence point
                    var latestEntrySequence = await dbContext.LedgerEntries
                        .Where(e => e.AccountId == account.Id)
                        .MaxAsync(e => e.JournalSequenceId, ct);

                    var balance = await dbContext.LedgerEntries
                        .Where(e => e.AccountId == account.Id && e.JournalSequenceId <= latestEntrySequence)
                        .SumAsync(e => e.Debit - e.Credit, ct);

                    var snapshot = new AccountBalanceSnapshot(
                        account.TenantId,
                        account.Id,
                        latestEntrySequence,
                        balance);

                    _ = dbContext.AccountBalanceSnapshots.Add(snapshot);
                }
            }

            _ = await unitOfWork.SaveChangesAsync(ct);
        }
    }
}

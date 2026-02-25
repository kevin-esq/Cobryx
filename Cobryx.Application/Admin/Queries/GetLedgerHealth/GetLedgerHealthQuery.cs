using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Admin.Queries.GetLedgerHealth;

public record GetLedgerHealthQuery : IRequest<Result<LedgerHealthDto>>;

public record LedgerHealthDto(
    string Status,
    int ImbalancedTransactionCount,
    int OrphanedEntryCount,
    List<string> Issues);

public class GetLedgerHealthHandler : IRequestHandler<GetLedgerHealthQuery, Result<LedgerHealthDto>>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly ILogger<GetLedgerHealthHandler> _logger;

    public GetLedgerHealthHandler(ICobryxDbContext dbContext, ILogger<GetLedgerHealthHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<LedgerHealthDto>> Handle(GetLedgerHealthQuery request, CancellationToken ct)
    {
        var issues = new List<string>();

        // 1. Check for Imbalanced Transactions (Sum(Debit) != Sum(Credit))
        // We group entries by TransactionId and check the balance.
        var imbalancedTxs = await _dbContext.LedgerEntries
            .GroupBy(e => e.TransactionId)
            .Select(g => new
            {
                TransactionId = g.Key,
                Balance = g.Sum(e => e.Debit - e.Credit)
            })
            .Where(x => x.Balance != 0)
            .ToListAsync(ct);

        if (imbalancedTxs.Any())
        {
            var msg = $"{imbalancedTxs.Count} imbalanced transactions detected.";
            issues.Add(msg);
            _logger.LogCritical("FINANCIAL CORRUPTION: {Message} Sample Tx IDs: {Ids}",
                msg, string.Join(", ", imbalancedTxs.Take(3).Select(x => x.TransactionId)));
        }

        // 2. Check for Orphaned Entries (Entries without a valid Transaction reference)
        // Note: EF Core usually prevents this via Foreign Keys, but for production "sealing", we check.
        var orphanedEntries = await _dbContext.LedgerEntries
            .Where(e => !_dbContext.LedgerTransactions.Select(t => t.Id).Contains(e.TransactionId))
            .CountAsync(ct);

        if (orphanedEntries > 0)
        {
            var msg = $"{orphanedEntries} orphaned ledger entries detected.";
            issues.Add(msg);
            _logger.LogError("LEDGER HEALTH: {Message}", msg);
        }

        // 3. Check for Entries with Invalid Account IDs
        var invalidAccountEntries = await _dbContext.LedgerEntries
            .Where(e => !_dbContext.LedgerAccounts.Select(a => a.Id).Contains(e.AccountId))
            .CountAsync(ct);

        if (invalidAccountEntries > 0)
        {
            var msg = $"{invalidAccountEntries} ledger entries referencing invalid Account IDs.";
            issues.Add(msg);
            _logger.LogError("LEDGER HEALTH: {Message}", msg);
        }

        var status = imbalancedTxs.Any() ? "CRITICAL" : (issues.Any() ? "DEGRADED" : "HEALTHY");

        return Result.Success(new LedgerHealthDto(
            Status: status,
            ImbalancedTransactionCount: imbalancedTxs.Count,
            OrphanedEntryCount: orphanedEntries + invalidAccountEntries,
            Issues: issues
        ));
    }
}

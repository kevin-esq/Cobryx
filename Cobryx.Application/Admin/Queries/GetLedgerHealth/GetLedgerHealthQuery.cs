using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

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

public class GetLedgerHealthHandler(ICobryxDbContext dbContext, ILogger<GetLedgerHealthHandler> logger)
    : IRequestHandler<GetLedgerHealthQuery, Result<LedgerHealthDto>>
{
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly ILogger<GetLedgerHealthHandler> _logger = logger;

    public async Task<Result<LedgerHealthDto>> Handle(GetLedgerHealthQuery request, CancellationToken ct)
    {
        var issues = new List<string>();

        var imbalancedTxs = await _dbContext.LedgerEntries
            .GroupBy(e => e.TransactionId)
            .Select(g => new
            {
                TransactionId = g.Key,
                Balance = g.Sum(e => e.Debit - e.Credit)
            })
            .Where(x => x.Balance != 0)
            .ToListAsync(ct);

        if (imbalancedTxs.Count > 0)
        {
            var msg = $"{imbalancedTxs.Count} imbalanced transactions detected.";
            issues.Add(msg);
            _logger.LogCritical("FINANCIAL CORRUPTION: {Message} Sample Tx IDs: {Ids}",
                msg, string.Join(", ", imbalancedTxs.Take(3).Select(x => x.TransactionId)));
        }

        var orphanedEntries = await _dbContext.LedgerEntries
            .Where(e => !_dbContext.LedgerTransactions.Select(t => t.Id).Contains(e.TransactionId))
            .CountAsync(ct);

        if (orphanedEntries > 0)
        {
            var msg = $"{orphanedEntries} orphaned ledger entries detected.";
            issues.Add(msg);
            _logger.LogError("LEDGER HEALTH: {Message}", msg);
        }

        var invalidAccountEntries = await _dbContext.LedgerEntries
            .Where(e => !_dbContext.LedgerAccounts.Select(a => a.Id).Contains(e.AccountId))
            .CountAsync(ct);

        if (invalidAccountEntries > 0)
        {
            var msg = $"{invalidAccountEntries} ledger entries referencing invalid Account IDs.";
            issues.Add(msg);
            _logger.LogError("LEDGER HEALTH: {Message}", msg);
        }

        var status = imbalancedTxs.Count > 0 ? "CRITICAL" : (issues.Count > 0 ? "DEGRADED" : "HEALTHY");

        return Result.Success(new LedgerHealthDto(
            Status: status,
            ImbalancedTransactionCount: imbalancedTxs.Count,
            OrphanedEntryCount: orphanedEntries + invalidAccountEntries,
            Issues: issues
        ));
    }
}

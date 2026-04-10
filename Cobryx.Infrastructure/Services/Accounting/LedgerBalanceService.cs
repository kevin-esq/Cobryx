using System.Diagnostics;

using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Accounting;

public class LedgerBalanceService : ILedgerBalanceService
{
    private readonly ICobryxDbContext _context;
    private readonly ICacheService _cache;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<LedgerBalanceService> _logger;

    public LedgerBalanceService(
        ICobryxDbContext context,
        ICacheService cache,
        CobryxMetrics metrics,
        ILogger<LedgerBalanceService> logger)
    {
        _context = context;
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
    }

    private static string GetCacheKey(Guid tenantId, Guid accountId) => $"ledger:balance:{tenantId}:{accountId}";

    public async Task<BalanceResult> GetBalanceAsync(Guid tenantId, Guid accountId, CancellationToken ct = default)
    {
        var key = GetCacheKey(tenantId, accountId);

        var cachedHash = await _cache.GetHashAllAsync(key, ct);

        var latestSnapshot = await _context.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.AccountId == accountId)
            .OrderByDescending(s => s.JournalSequenceId)
            .FirstOrDefaultAsync(ct);

        if (cachedHash != null &&
            cachedHash.TryGetValue("balance", out var bStr) &&
            cachedHash.TryGetValue("sequence", out var sStr))
        {
            if (decimal.TryParse(bStr, out var bVal) && long.TryParse(sStr, out var sVal))
            {
                if (latestSnapshot == null || sVal >= latestSnapshot.JournalSequenceId)
                {
                    _metrics.BalanceCacheHits.Add(1);
                    return new BalanceResult(bVal, sVal);
                }
            }
        }

        _metrics.BalanceCacheMisses.Add(1);
        _metrics.BalanceCacheRecomputeTotal.Add(1);

        var sw = Stopwatch.StartNew();

        var baseBalance = latestSnapshot?.Balance ?? 0m;
        var baseSequence = latestSnapshot?.JournalSequenceId ?? -1L;

        if (latestSnapshot != null)
        {
            var age = (DateTime.UtcNow - latestSnapshot.CreatedAt).TotalSeconds;
            _metrics.LedgerSnapshotAge.Record(age);
        }

        var deltaSw = Stopwatch.StartNew();
        var delta = await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.AccountId == accountId && e.JournalSequenceId > baseSequence)
            .SumAsync(e => e.Debit - e.Credit, ct);
        deltaSw.Stop();
        _metrics.LedgerDeltaScanDuration.Record(deltaSw.Elapsed.TotalSeconds);

        var finalBalance = baseBalance + delta;

        var latestEntrySequence = await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.AccountId == accountId)
            .MaxAsync(e => (long?)e.JournalSequenceId, ct) ?? baseSequence;

        var result = new BalanceResult(finalBalance, latestEntrySequence);

        await UpdateCacheInternalAsync(tenantId, accountId, finalBalance, latestEntrySequence, ct);

        sw.Stop();
        _metrics.BalanceCacheRebuildDuration.Record(sw.Elapsed.TotalSeconds);

        return result;
    }

    public async Task<BalanceResult> GetHistoricalBalanceAsync(Guid tenantId, Guid accountId, long journalSequenceId, CancellationToken ct = default)
    {
        var snapshot = await _context.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.AccountId == accountId && s.JournalSequenceId <= journalSequenceId)
            .OrderByDescending(s => s.JournalSequenceId)
            .FirstOrDefaultAsync(ct);

        var baseBalance = snapshot?.Balance ?? 0m;
        var baseSequence = snapshot?.JournalSequenceId ?? -1L;

        var delta = await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.AccountId == accountId && e.JournalSequenceId > baseSequence && e.JournalSequenceId <= journalSequenceId)
            .SumAsync(e => e.Debit - e.Credit, ct);

        return new BalanceResult(baseBalance + delta, journalSequenceId);
    }

    public async Task UpdateCacheAsync(Guid tenantId, Guid accountId, decimal balanceChange, long newSequenceId, CancellationToken ct = default)
    {
        var key = GetCacheKey(tenantId, accountId);

        var cached = await _cache.GetHashAllAsync(key, ct);
        decimal currentBalance = 0;

        if (cached != null && cached.TryGetValue("balance", out var bStr) && decimal.TryParse(bStr, out var bVal))
        {
            currentBalance = bVal;
        }
        else
        {
            await GetBalanceAsync(tenantId, accountId, ct);
            return;
        }

        var newBalance = currentBalance + balanceChange;

        var fields = new Dictionary<string, string>
        {
            { "balance", newBalance.ToString() },
            { "sequence", newSequenceId.ToString() },
            { "updated_at", DateTime.UtcNow.ToString("o") }
        };

        var updated = await _cache.TryAtomicHashUpdateIfNewerAsync(key, fields, newSequenceId, "sequence", TimeSpan.FromHours(24), ct);

        if (!updated)
        {
            _metrics.LedgerCacheAtomicRejectTotal.Add(1);
        }
    }

    private async Task UpdateCacheInternalAsync(Guid tenantId, Guid accountId, decimal balance, long sequence, CancellationToken ct)
    {
        var key = GetCacheKey(tenantId, accountId);
        var fields = new Dictionary<string, string>
        {
            { "balance", balance.ToString() },
            { "sequence", sequence.ToString() },
            { "updated_at", DateTime.UtcNow.ToString("o") }
        };

        await _cache.TryAtomicHashUpdateIfNewerAsync(key, fields, sequence, "sequence", TimeSpan.FromHours(24), ct);
    }

    public async Task RebuildAccountSnapshotAsync(Guid tenantId, Guid accountId, long sequenceId, CancellationToken ct = default)
    {
        var historical = await GetHistoricalBalanceAsync(tenantId, accountId, sequenceId, ct);

        var snapshot = new AccountBalanceSnapshot(tenantId, accountId, sequenceId, historical.Balance);
        _context.AccountBalanceSnapshots.Add(snapshot);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Rebuilt snapshot for account {AccountId} at sequence {SequenceId}", accountId, sequenceId);
    }

    public async Task<bool> VerifyBalanceIntegrityAsync(Guid tenantId, Guid accountId, CancellationToken ct = default)
    {
        var deepSum = await _context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.AccountId == accountId)
            .SumAsync(e => e.Debit - e.Credit, ct);

        var current = await GetBalanceAsync(tenantId, accountId, ct);

        return Math.Abs(deepSum - current.Balance) < 0.001m;
    }

    public async Task WarmupCacheAsync(Guid tenantId, CancellationToken ct = default)
    {
        var accountIds = await _context.LedgerAccounts
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        foreach (var accountId in accountIds)
        {
            await GetBalanceAsync(tenantId, accountId, ct);
        }
    }
}

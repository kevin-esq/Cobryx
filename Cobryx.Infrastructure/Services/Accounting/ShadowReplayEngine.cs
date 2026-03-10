using Cobryx.Application.Accounting.Events;
using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Accounting;

public class ShadowReplayEngine : IShadowReplayEngine
{
    private readonly ICobryxDbContext _context;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<ShadowReplayEngine> _logger;

    public ShadowReplayEngine(
        ICobryxDbContext context,
        CobryxMetrics metrics,
        ILogger<ShadowReplayEngine> _logger)
    {
        _context = context;
        _metrics = metrics;
        this._logger = _logger;
    }

    public async Task ProcessEventAsync(LedgerCdcEvent cdcEvent, CancellationToken ct = default)
    {
        var shadow = await _context.ShadowBalances
            .FirstOrDefaultAsync(s => s.TenantId == cdcEvent.TenantId && s.AccountId == cdcEvent.AccountId, ct);

        if (shadow == null)
        {
            var amount = cdcEvent.DebitAmount - cdcEvent.CreditAmount;
            shadow = new ShadowBalance(cdcEvent.TenantId, cdcEvent.AccountId, amount, cdcEvent.JournalSequenceId);
            _context.ShadowBalances.Add(shadow);
        }
        else
        {
            var amount = cdcEvent.DebitAmount - cdcEvent.CreditAmount;
            shadow.ApplyChange(amount, cdcEvent.JournalSequenceId);
        }

        await _context.SaveChangesAsync(ct);

        _metrics.ShadowReplayEventsProcessed.Add(1);
        _metrics.ShadowReplayThroughput.Add(1);
    }

    public async Task<long> GetLastSequenceAsync(CancellationToken ct = default)
    {
        return await _context.ShadowBalances.MaxAsync(s => (long?)s.LastSequence, ct) ?? -1L;
    }
}

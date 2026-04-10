using Cobryx.Application.Accounting.Events;
using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services.Accounting;

public class ShadowReplayEngine(
    ICobryxDbContext context,
    CobryxMetrics metrics) : IShadowReplayEngine
{

    public async Task ProcessEventAsync(LedgerCdcEvent cdcEvent, CancellationToken ct = default)
    {
        ShadowBalance? shadow = await context.ShadowBalances
            .FirstOrDefaultAsync(s => s.TenantId == cdcEvent.TenantId && s.AccountId == cdcEvent.AccountId, ct);

        if (shadow == null)
        {
            decimal amount = cdcEvent.DebitAmount - cdcEvent.CreditAmount;
            shadow = new ShadowBalance(cdcEvent.TenantId, cdcEvent.AccountId, amount, cdcEvent.JournalSequenceId);
            context.ShadowBalances.Add(shadow);
        }
        else
        {
            decimal changeAmount = cdcEvent.DebitAmount - cdcEvent.CreditAmount;
            shadow.ApplyChange(changeAmount, cdcEvent.JournalSequenceId);
        }

        await context.SaveChangesAsync(ct);

        metrics.ShadowReplayEventsProcessed.Add(1);
        metrics.ShadowReplayThroughput.Add(1);
    }

    public async Task<long> GetLastSequenceAsync(CancellationToken ct = default)
    {
        return await context.ShadowBalances.MaxAsync(s => (long?)s.LastSequence, ct) ?? -1L;
    }
}

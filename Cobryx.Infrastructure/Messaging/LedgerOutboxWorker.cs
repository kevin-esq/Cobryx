using System.Text.Json;

using Cobryx.Application.Accounting.Events;
using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Messaging;

public class LedgerOutboxWorker(
    ICobryxDbContext context,
    ILedgerBalanceService balanceService,
    IShadowReplayEngine shadowEngine,
    ILedgerPublisher publisher,
    ILogger<LedgerOutboxWorker> logger)
{
    private readonly ICobryxDbContext _context = context;
    private readonly ILedgerBalanceService _balanceService = balanceService;
    private readonly IShadowReplayEngine _shadowEngine = shadowEngine;
    private readonly ILedgerPublisher _publisher = publisher;
    private readonly ILogger<LedgerOutboxWorker> _logger = logger;

    [Queue("ledger")]
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessEventsAsync(CancellationToken ct)
    {
        var events = await _context.OutboxMessages
            .Where(o => !o.IsProcessed && o.LedgerSequenceId != null)
            .OrderBy(o => o.LedgerSequenceId)
            .Take(100)
            .ToListAsync(ct);

        if (events.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} ledger outbox events...", events.Count);

        var cdcEvents = new List<LedgerCdcEvent>();
        foreach (var outbox in events)
        {
            try
            {
                var cdcEvent = JsonSerializer.Deserialize<LedgerCdcEvent>(outbox.Payload);
                if (cdcEvent != null)
                {
                    cdcEvents.Add(cdcEvent);

                    await _balanceService.UpdateCacheAsync(
                        cdcEvent.TenantId,
                        cdcEvent.AccountId,
                        cdcEvent.DebitAmount - cdcEvent.CreditAmount,
                        cdcEvent.JournalSequenceId,
                        ct);

                    await _shadowEngine.ProcessEventAsync(cdcEvent, ct);
                }
                outbox.MarkAsProcessed(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize ledger outbox event {SequenceId}", outbox.LedgerSequenceId);
            }
        }

        if (cdcEvents.Count > 0)
        {
            await _publisher.PublishAsync(cdcEvents, ct);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Successfully processed {Count} ledger outbox events.", cdcEvents.Count);
    }
}

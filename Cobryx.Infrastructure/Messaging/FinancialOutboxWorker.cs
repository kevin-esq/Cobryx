using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Messaging;

/// <summary>
/// Background worker that processes the Financial Outbox.
/// Features: Batching (500), Retries (10), Dead Letter Queue (DLQ), and Atomic Transactional Safety.
/// </summary>
public class FinancialOutboxWorker(
    ICobryxDbContext context,
    IFinancialEventBus eventBus,
    ILogger<FinancialOutboxWorker> logger)
{
    private readonly ICobryxDbContext _context = context;
    private readonly IFinancialEventBus _eventBus = eventBus;
    private readonly ILogger<FinancialOutboxWorker> _logger = logger;

    [Queue("financial-events")]
    [AutomaticRetry(Attempts = 0)] // We handle retries internally via RetryCount
    public async Task ProcessEventsAsync(CancellationToken ct)
    {
        var outboxEvents = await _context.OutboxMessages
            .Where(e => !e.IsProcessed && e.RetryCount < 10 && e.LedgerSequenceId != null)
            .OrderBy(e => e.LedgerSequenceId)
            .Take(500)
            .ToListAsync(ct);

        if (outboxEvents.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} financial outbox events...", outboxEvents.Count);

        try
        {
            await _eventBus.PublishBatchAsync(outboxEvents, ct);

            foreach (var @event in outboxEvents)
            {
                @event.MarkAsProcessed(DateTime.UtcNow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch of financial events. Incrementing retries.");

            foreach (var @event in outboxEvents)
            {
                @event.IncrementRetry();

                if (@event.RetryCount >= 10)
                {
                    _logger.LogCritical("Event {EventId} reached max retries. Moving to Dead Letter Queue.", @event.Id);
                    var dlq = new DeadLetterEvent(@event, ex.Message);
                    _context.DeadLetterEvents.Add(dlq);
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Batch processing completed. Published: {Published}", outboxEvents.Count(e => e.IsProcessed));
    }
}

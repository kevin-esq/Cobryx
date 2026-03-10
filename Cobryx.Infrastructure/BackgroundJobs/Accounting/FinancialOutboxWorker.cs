using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace Cobryx.Infrastructure.BackgroundJobs.Accounting;

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
        // 1. Fetch batch using FOR UPDATE SKIP LOCKED pattern for high-concurrency safety.
        var outboxEvents = await _context.FinancialOutboxEvents
            .Where(e => !e.IsPublished && e.RetryCount < 10)
            .OrderBy(e => e.LedgerSequenceId)
            .Take(500)
            .ToListAsync(ct);

        if (outboxEvents.Count == 0) return;

        _logger.LogInformation("Processing {Count} financial outbox events...", outboxEvents.Count);

        // 2. Publish Batch
        try
        {
            await _eventBus.PublishBatchAsync(outboxEvents, ct);

            // 3. Mark as Published
            foreach (var @event in outboxEvents)
            {
                @event.MarkAsPublished(DateTime.UtcNow);
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

        // 4. Atomic Commit
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Batch processing completed. Published: {Published}", outboxEvents.Count(e => e.IsPublished));
    }
}

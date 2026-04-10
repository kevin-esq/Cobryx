using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Messaging;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Messaging
{
    /// <summary>
    /// Background worker that processes the Financial Outbox.
    /// Features: Batching (500), Retries (10), Dead Letter Queue (DLQ), and Atomic Transactional Safety.
    /// </summary>
    public partial class FinancialOutboxWorker(
        ICobryxDbContext context,
        IFinancialEventBus eventBus,
        ILogger<FinancialOutboxWorker> logger)
    {
        [Queue("financial-events")]
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessEventsAsync(CancellationToken ct)
        {
            List<OutboxMessage> outboxEvents = await context.OutboxMessages
                .Where(e => !e.IsProcessed && e.RetryCount < 10 && e.LedgerSequenceId != null)
                .OrderBy(e => e.LedgerSequenceId)
                .Take(500)
                .ToListAsync(ct);

            if (outboxEvents.Count == 0)
            {
                return;
            }

            LogProcessingEvents(logger, outboxEvents.Count);

            try
            {
                await eventBus.PublishBatchAsync(outboxEvents, ct);

                foreach (OutboxMessage @event in outboxEvents)
                {
                    @event.MarkAsProcessed(DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                LogPublishFailed(logger, ex);

                foreach (OutboxMessage @event in outboxEvents)
                {
                    @event.IncrementRetry();

                    if (@event.RetryCount >= 10)
                    {
                        LogMaxRetriesReached(logger, @event.Id);
                        DeadLetterEvent dlq = new(@event, ex.Message);
                        _ = context.DeadLetterEvents.Add(dlq);
                    }
                }
            }

            _ = await context.SaveChangesAsync(ct);

            LogBatchCompleted(logger, outboxEvents.Count(e => e.IsProcessed));
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} financial outbox events...")]
        private static partial void LogProcessingEvents(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Information, Message = "Batch processing completed. Published: {Published}")]
        private static partial void LogBatchCompleted(ILogger logger, int published);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to publish batch of financial events. Incrementing retries.")]
        private static partial void LogPublishFailed(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Critical, Message = "Event {EventId} reached max retries. Moving to Dead Letter Queue.")]
        private static partial void LogMaxRetriesReached(ILogger logger, Guid eventId);
    }
}

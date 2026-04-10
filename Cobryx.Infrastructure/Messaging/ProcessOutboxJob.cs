using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Messaging;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.Persistence;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Cobryx.Infrastructure.Messaging;

public partial class ProcessOutboxJob(
    IServiceProvider serviceProvider,
    ILogger<ProcessOutboxJob> logger) : BackgroundService
{
    private const int MaxRetries = 10;
    private const int DlqAlertThreshold = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox events.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        CobryxDbContext dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        CobryxMetrics metrics = scope.ServiceProvider.GetRequiredService<CobryxMetrics>();

        List<OutboxMessage> events = await dbContext.OutboxMessages
            .Where(m => !m.IsProcessed && m.LedgerSequenceId == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .ThenBy(m => m.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (OutboxMessage outboxEvent in events)
        {
            try
            {
                logger.LogInformation("Processing outbox event: {Type} (CorrelationId: {CorrelationId})",
                    outboxEvent.Type, outboxEvent.CorrelationId);

                using IDisposable correlationContext = Serilog.Context.LogContext.PushProperty("CorrelationId", outboxEvent.CorrelationId);
                System.Diagnostics.Activity.Current?.AddTag("CorrelationId", outboxEvent.CorrelationId);

                double lag = (DateTime.UtcNow - outboxEvent.OccurredOnUtc).TotalSeconds;
                metrics.OutboxProcessingLag.Record(lag, new KeyValuePair<string, object?>("Type", outboxEvent.Type));

                IDomainEvent? domainEvent = DeserializeDomainEvent(outboxEvent);
                if (domainEvent != null)
                {
                    Type notificationType = typeof(Application.Common.Events.DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    INotification? notification = Activator.CreateInstance(notificationType, domainEvent) as INotification;

                    if (notification != null)
                    {
                        await mediator.Publish(notification, cancellationToken);
                    }
                }

                outboxEvent.MarkAsProcessed(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox event {Id}", outboxEvent.Id);
                outboxEvent.MarkAsFailed(ex.Message);

                // Move to DLQ after max retries
                if (outboxEvent.RetryCount >= MaxRetries)
                {
                    LogMovingToDlq(logger, outboxEvent.Id, outboxEvent.Type);

                    var dlqEvent = new DeadLetterEvent(outboxEvent, ex.Message);
                    dbContext.DeadLetterEvents.Add(dlqEvent);

                    // Mark as processed to stop retrying
                    outboxEvent.MarkAsProcessed(DateTime.UtcNow);

                    metrics.DeadLetterCount.Add(1, new KeyValuePair<string, object?>("Type", outboxEvent.Type));
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Alert if DLQ is growing
        await CheckDlqThresholdAsync(dbContext, metrics, cancellationToken);
    }

    private async Task CheckDlqThresholdAsync(CobryxDbContext dbContext, CobryxMetrics metrics, CancellationToken ct)
    {
        var dlqCount = await dbContext.DeadLetterEvents
            .Where(d => d.CreatedAt > DateTime.UtcNow.AddHours(-24))
            .CountAsync(ct);

        if (dlqCount > DlqAlertThreshold)
        {
            LogDlqThresholdExceeded(logger, dlqCount, DlqAlertThreshold);
            metrics.DeadLetterAlertTriggered.Add(1);
        }
    }

    private IDomainEvent? DeserializeDomainEvent(OutboxMessage outboxEvent)
    {
        try
        {
            // Use TypeNameHandling.Auto with known types for security
            return JsonConvert.DeserializeObject<IDomainEvent>(outboxEvent.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new KnownDomainEventsBinder()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deserializing outbox event {Id}", outboxEvent.Id);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "Event {EventId} ({Type}) reached max retries. Moving to Dead Letter Queue.")]
    private static partial void LogMovingToDlq(ILogger logger, Guid eventId, string type);

    [LoggerMessage(Level = LogLevel.Critical, Message = "DLQ threshold exceeded: {Count} events in last 24h (threshold: {Threshold})")]
    private static partial void LogDlqThresholdExceeded(ILogger logger, int count, int threshold);
}

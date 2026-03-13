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

public class ProcessOutboxJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessOutboxJob> _logger;

    public ProcessOutboxJob(IServiceProvider serviceProvider, ILogger<ProcessOutboxJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var events = await dbContext.OutboxMessages
            .Where(m => !m.IsProcessed && m.LedgerSequenceId == null) // Process generic domain events only
            .OrderBy(m => m.OccurredOnUtc)
            .ThenBy(m => m.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var outboxEvent in events)
        {
            try
            {
                _logger.LogInformation("Processing outbox event: {Type} (CorrelationId: {CorrelationId})",
                    outboxEvent.Type, outboxEvent.CorrelationId);

                using var correlationContext = Serilog.Context.LogContext.PushProperty("CorrelationId", outboxEvent.CorrelationId);
                System.Diagnostics.Activity.Current?.AddTag("CorrelationId", outboxEvent.CorrelationId);

                var lag = (DateTime.UtcNow - outboxEvent.OccurredOnUtc).TotalSeconds;
                var metrics = scope.ServiceProvider.GetRequiredService<Cobryx.Application.Common.Observability.CobryxMetrics>();
                metrics.OutboxProcessingLag.Record(lag, new KeyValuePair<string, object?>("Type", outboxEvent.Type));

                var domainEvent = DeserializeDomainEvent(outboxEvent);
                if (domainEvent != null)
                {
                    var notificationType = typeof(Cobryx.Application.Common.Events.DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification = Activator.CreateInstance(notificationType, domainEvent) as INotification;

                    if (notification != null)
                    {
                        await mediator.Publish(notification, cancellationToken);
                    }
                }

                outboxEvent.MarkAsProcessed(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox event {Id}", outboxEvent.Id);
                outboxEvent.MarkAsFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IDomainEvent? DeserializeDomainEvent(OutboxMessage outboxEvent)
    {
        try
        {
            return JsonConvert.DeserializeObject<IDomainEvent>(outboxEvent.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing outbox event {Id}", outboxEvent.Id);
            return null;
        }
    }
}

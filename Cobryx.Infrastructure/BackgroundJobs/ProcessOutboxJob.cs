using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Infrastructure.Persistence;
using Concordia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

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
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation("Processing outbox message: {Type}", message.Type);

                var domainEvent = DeserializeDomainEvent(message);
                if (domainEvent != null)
                {
                    var notificationType = typeof(Cobryx.Application.Common.Events.DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification = Activator.CreateInstance(notificationType, domainEvent);
                    
                    if (notification != null)
                    {
                        await sender.Send(notification, stoppingToken);
                    }
                }

                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                message.MarkAsFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private IDomainEvent? DeserializeDomainEvent(OutboxMessage message)
    {
        var type = Type.GetType($"Cobryx.Domain.Events.{message.Type}, Cobryx.Domain");
        if (type == null) return null;

        return JsonSerializer.Deserialize(message.Content, type) as IDomainEvent;
    }
}

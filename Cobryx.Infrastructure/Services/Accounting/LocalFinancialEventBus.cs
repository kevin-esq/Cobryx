using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Accounting;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Accounting;

/// <summary>
/// Implementation of the Financial Event Bus for local/in-process execution.
/// Dispatches financial semantic events to registered consumers.
/// </summary>
public class LocalFinancialEventBus(
    IEnumerable<IFinancialEventConsumer> consumers,
    ILogger<LocalFinancialEventBus> logger) : IFinancialEventBus
{
    private readonly IEnumerable<IFinancialEventConsumer> _consumers = consumers;
    private readonly ILogger<LocalFinancialEventBus> _logger = logger;

    public async Task PublishAsync(FinancialOutboxEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation("Publishing Financial Event {EventId} (Type: {Type}, Partition: {Partition})",
            @event.Id, @event.Type, @event.PartitionKey);

        foreach (var consumer in _consumers)
        {
            await consumer.ConsumeAsync(@event, ct);
        }
    }

    public async Task PublishBatchAsync(IEnumerable<FinancialOutboxEvent> events, CancellationToken ct = default)
    {
        var count = events.Count();
        _logger.LogInformation("Publishing Batch of {Count} Financial Events", count);

        foreach (var @event in events)
        {
            await PublishAsync(@event, ct);
        }
    }
}

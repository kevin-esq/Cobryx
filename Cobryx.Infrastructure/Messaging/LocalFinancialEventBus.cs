using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Messaging;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Messaging;

/// <summary>
/// Implementation of the Financial Event Bus for local/in-process execution.
/// Dispatches financial semantic events to registered consumers.
/// </summary>
public class LocalFinancialEventBus(
    IEnumerable<IFinancialEventConsumer> consumers,
    ILogger<LocalFinancialEventBus> logger) : IFinancialEventBus
{

    public async Task PublishAsync(OutboxMessage @event, CancellationToken ct = default)
    {
        logger.LogInformation("Publishing Financial Event {EventId} (Type: {Type}, Partition: {Partition})",
            @event.Id, @event.Type, @event.PartitionKey);

        foreach (IFinancialEventConsumer consumer in consumers)
        {
            await consumer.ConsumeAsync(@event, ct);
        }
    }

    public async Task PublishBatchAsync(IEnumerable<OutboxMessage> events, CancellationToken ct = default)
    {
        List<OutboxMessage> eventList = events.ToList();
        logger.LogInformation("Publishing Batch of {Count} Financial Events", eventList.Count);

        foreach (OutboxMessage @event in eventList)
        {
            await PublishAsync(@event, ct);
        }
    }
}

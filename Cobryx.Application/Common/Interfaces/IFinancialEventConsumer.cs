using Cobryx.Domain.Messaging;

namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Abstraction for a consumer of the Unified Financial Event Pipeline.
/// Consumers use this to process semantic events independently.
/// </summary>
public interface IFinancialEventConsumer
{
    /// <summary>
    /// Processes a semantic financial event.
    /// Implementation must ensure idempotency using ProcessedEvent tracking.
    /// </summary>
    public Task ConsumeAsync(OutboxMessage @event, CancellationToken ct = default);

    /// <summary>
    /// Unique name of the consumer for idempotency tracking.
    /// </summary>
    public string Name { get; }
}

namespace Cobryx.Application.Common.Interfaces;

using Cobryx.Domain.Messaging;

/// <summary>
/// Domain-level abstraction for the Financial Event Bus.
/// Partitioned by TenantId to ensure sequential ordering within a tenant.
/// </summary>
public interface IFinancialEventBus
{
    /// <summary>
    /// Publishes a financial event to the bus.
    /// This should be called by the Outbox Worker.
    /// </summary>
    public Task PublishAsync(OutboxMessage @event, CancellationToken ct = default);

    /// <summary>
    /// Batch publish for performance.
    /// </summary>
    public Task PublishBatchAsync(IEnumerable<OutboxMessage> events, CancellationToken ct = default);
}

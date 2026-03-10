using Cobryx.Domain.Entities.Accounting;

namespace Cobryx.Application.Common.Interfaces;

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
    Task PublishAsync(FinancialOutboxEvent @event, CancellationToken ct = default);

    /// <summary>
    /// Batch publish for performance.
    /// </summary>
    Task PublishBatchAsync(IEnumerable<FinancialOutboxEvent> events, CancellationToken ct = default);
}

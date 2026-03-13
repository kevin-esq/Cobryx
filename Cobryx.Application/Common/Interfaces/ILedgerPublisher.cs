using Cobryx.Application.Accounting.Events;

namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Domain-neutral interface for publishing ledger events to an external stream (Kafka, RabbitMQ, etc.).
/// </summary>
public interface ILedgerPublisher
{
    /// <summary>
    /// Publishes a batch of ledger entries as CDC events.
    /// Implementation should ensure partitioning by TenantId for intra-partition ordering.
    /// </summary>
    public Task PublishAsync(IEnumerable<LedgerCdcEvent> events, CancellationToken ct = default);

    /// <summary>
    /// Publishes a single ledger event. 
    /// </summary>
    public Task PublishAsync(LedgerCdcEvent @event, CancellationToken ct = default);
}

using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Events.Accounting;

/// <summary>
/// Forensic event emitted when a tenant is placed into Financial Safe Mode.
/// Mandatory for high-stakes financial audit trails.
/// </summary>
/// <param name="TenantId">The affected tenant.</param>
/// <param name="Reason">Why safe mode was tripped.</param>
/// <param name="TriggeredBy">How it was triggered (SYSTEM, DETECTION, MANUAL).</param>
/// <param name="CorrelationId">Links detection to the original drift event.</param>
/// <param name="OccurredOn">Exact timestamp when the event occurred (IDomainEvent contract).</param>
public record FinancialSafeModeTrippedEvent(
    Guid TenantId,
    string Reason,
    string TriggeredBy,
    string CorrelationId,
    DateTime OccurredOn) : IDomainEvent;

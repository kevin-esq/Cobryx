namespace Cobryx.Application.Accounting.Events;

/// <summary>
/// Explicit Ledger Event DTO for CDC (Change Data Capture) serialization.
/// Designed for high-fidelity shadow replay and auditing.
/// </summary>
public record LedgerCdcEvent(
    long JournalSequenceId,
    Guid TenantId,
    Guid TransactionId,
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string Currency,
    DateTime EffectiveAt,
    DateTime CreatedAt,
    long EntryVersion,
    string ReferenceId
);

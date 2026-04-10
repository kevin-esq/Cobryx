using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting;

/// <summary>
/// Represents a single line (Debit or Credit) in a Journal Transaction.
/// </summary>
public class LedgerEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public long JournalSequenceId { get; private set; }

    public string Currency { get; private set; } = CobryxDefaults.Currency;
    public string ReferenceId { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }

    private LedgerEntry() { }

    internal LedgerEntry(Guid tenantId, Guid transactionId, Guid accountId, decimal debit, decimal credit, string? currency = null, string referenceId = "")
    {
        TenantId = tenantId;
        TransactionId = transactionId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Currency = currency ?? CobryxDefaults.Currency;
        ReferenceId = referenceId;
    }

    internal void AdjustDebit(decimal delta)
    {
        Debit += delta;
    }

    internal void AdjustCredit(decimal delta)
    {
        Credit += delta;
    }
}

using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// Represents a single line (Debit or Credit) in a Journal Transaction.
/// </summary>
public class LedgerEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public long JournalSequenceId { get; private set; }

    public string Currency { get; private set; } = "USD";
    public string ReferenceId { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }

    private LedgerEntry() { } // EF Core

    internal LedgerEntry(Guid tenantId, Guid transactionId, Guid accountId, decimal debit, decimal credit, string currency = "USD", string referenceId = "")
    {
        TenantId = tenantId;
        TransactionId = transactionId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Currency = currency;
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

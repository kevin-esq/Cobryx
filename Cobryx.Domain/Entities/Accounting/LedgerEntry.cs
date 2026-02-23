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

    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }

    private LedgerEntry() { } // EF Core

    internal LedgerEntry(Guid tenantId, Guid transactionId, Guid accountId, decimal debit, decimal credit)
    {
        TenantId = tenantId;
        TransactionId = transactionId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
    }
}

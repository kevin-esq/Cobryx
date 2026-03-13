using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Lending;

/// <summary>
/// Audit-safe record of a daily accrued specialized charge (Interest, Late Fees).
/// These charges are later materialized into Ledger entries.
/// </summary>
public class AccruedCharge : BaseEntity
{
    public Guid LoanId { get; private set; }
    public ChargeType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime AccrualDate { get; private set; }
    public bool PostedToLedger { get; private set; }
    public long? LedgerSequenceId { get; private set; }

    public virtual Loan Loan { get; private set; } = null!;

    private AccruedCharge() { }

    public AccruedCharge(Guid loanId, ChargeType type, decimal amount, DateTime accrualDate)
    {
        if (loanId == Guid.Empty) throw new ArgumentException("LoanId is required", nameof(loanId));
        if (amount < 0) throw new ArgumentException("Amount cannot be negative", nameof(amount));

        LoanId = loanId;
        Type = type;
        Amount = amount;
        AccrualDate = accrualDate.Date;
        PostedToLedger = false;
    }

    public void MarkAsPosted(long sequenceId)
    {
        PostedToLedger = true;
        LedgerSequenceId = sequenceId;
        UpdateTimestamp();
    }
}

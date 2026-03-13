using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting;

/// <summary>
/// A Journal Entry in the system. 
/// Groups multiple LedgerEntries into an atomic, balanced unit of work.
/// </summary>
public class LedgerTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime EffectiveDate { get; private set; }
    public bool IsPosted { get; private set; }
    public bool IsReversal { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }
    public Guid? LoanId { get; private set; }

    private readonly List<LedgerEntry> _entries = new();
    public virtual IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    private LedgerTransaction() { }

    public LedgerTransaction(Guid tenantId, string description, string? referenceId = null, Guid? loanId = null, string currency = "USD")
    {
        TenantId = tenantId;
        Description = description;
        ReferenceId = referenceId;
        LoanId = loanId;
        Currency = currency;
        EffectiveDate = DateTime.UtcNow;
        IsPosted = false;
    }

    public void AddEntry(Guid accountId, decimal debit, decimal credit)
    {
        if (IsPosted) throw new DomainException(DomainErrorCode.Common.GeneralError);

        _entries.Add(new LedgerEntry(TenantId, Id, accountId, debit, credit, Currency, ReferenceId ?? string.Empty));
    }

    public void Post()
    {
        if (IsPosted) return;

        // Double-Entry Integrity Check
        var balance = _entries.Sum(e => e.Debit - e.Credit);
        if (balance != 0)
        {
            throw new DomainException(DomainErrorCode.Common.GeneralError); // "Ledger out of balance"
        }

        IsPosted = true;
        UpdateTimestamp();
    }

    public static LedgerTransaction CreateReversal(LedgerTransaction original, string reason)
    {
        return CreatePartialReversal(original, original.Entries.Sum(e => e.Debit), reason);
    }

    public static LedgerTransaction CreatePartialReversal(LedgerTransaction original, decimal refundAmount, string reason)
    {
        var totalOriginal = original.Entries.Sum(e => Math.Abs(e.Debit));
        if (totalOriginal <= 0)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        // Small delta check for safety
        if (refundAmount > (totalOriginal / 2 + 0.01m) && original.Entries.Count == 2)
        {
            // If it's a simple 2-line transaction, we can be stricter,
        }

        if (refundAmount > totalOriginal + 0.01m)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        var ratio = refundAmount / totalOriginal;
        var reversalId = original.ReferenceId != null ? $"REV-PRT-{Guid.NewGuid().ToString().Substring(0, 8)}-{original.ReferenceId}" : null;

        var reversal = new LedgerTransaction(original.TenantId, reason, reversalId, original.LoanId)
        {
            IsReversal = true,
            OriginalTransactionId = original.Id
        };

        foreach (var entry in original.Entries)
        {
            // Mirror: original Credit becomes Reversal Debit, original Debit becomes Reversal Credit
            reversal.AddEntry(entry.AccountId, Math.Round(entry.Credit * ratio, 2), Math.Round(entry.Debit * ratio, 2));
        }

        // Fix balance if off by cents due to rounding
        var balance = reversal._entries.Sum(e => e.Debit - e.Credit);
        if (balance != 0)
        {
            // Adjust the largest entry to minimize relative impact of rounding
            var entryToAdjust = reversal._entries.OrderByDescending(e => Math.Abs(e.Debit + e.Credit)).First();
            if (balance > 0)
                entryToAdjust.AdjustCredit(balance);
            else
                entryToAdjust.AdjustDebit(-balance);
        }

        reversal.Post();
        return reversal;
    }
}

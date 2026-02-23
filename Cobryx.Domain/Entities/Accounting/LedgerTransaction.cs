using Cobryx.Domain.Common;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// A Journal Entry in the system. 
/// Groups multiple LedgerEntries into an atomic, balanced unit of work.
/// </summary>
public class LedgerTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceId { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public bool IsPosted { get; private set; }
    public bool IsReversal { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }

    private readonly List<LedgerEntry> _entries = new();
    public virtual IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    private LedgerTransaction() { }

    public LedgerTransaction(Guid tenantId, string description, string? referenceId = null)
    {
        TenantId = tenantId;
        Description = description;
        ReferenceId = referenceId;
        EffectiveDate = DateTime.UtcNow;
        IsPosted = false;
    }

    public void AddEntry(Guid accountId, decimal debit, decimal credit)
    {
        if (IsPosted) throw new DomainException(DomainErrorCode.Common.GeneralError);

        _entries.Add(new LedgerEntry(TenantId, Id, accountId, debit, credit));
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
        var reversalId = original.ReferenceId != null ? $"REV-{original.ReferenceId}" : null;
        var reversal = new LedgerTransaction(original.TenantId, reason, reversalId)
        {
            IsReversal = true,
            OriginalTransactionId = original.Id
        };

        foreach (var entry in original.Entries)
        {
            // Mirror bits: Debit becomes Credit, Credit becomes Debit
            reversal.AddEntry(entry.AccountId, entry.Credit, entry.Debit);
        }

        reversal.Post();
        return reversal;
    }
}

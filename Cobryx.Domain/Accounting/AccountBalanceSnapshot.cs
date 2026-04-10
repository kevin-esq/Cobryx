using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting;

/// <summary>
/// A materialized checkpoint of an account balance at a specific point in the journal.
/// Used for O(1) balance lookups and as a base for Shadow Replay verification.
/// </summary>
public class AccountBalanceSnapshot : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid AccountId { get; private set; }
    public long JournalSequenceId { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsVerified { get; private set; }

    private AccountBalanceSnapshot() { }

    public AccountBalanceSnapshot(Guid tenantId, Guid accountId, long journalSequenceId, decimal balance)
    {
        TenantId = tenantId;
        AccountId = accountId;
        JournalSequenceId = journalSequenceId;
        Balance = balance;
        IsVerified = false;
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        UpdateTimestamp();
    }
}

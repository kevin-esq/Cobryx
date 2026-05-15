using System.Reflection;
using Cobryx.Domain.Accounting;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Tests.Chaos;

/// <summary>
/// Elite chaos utility to bypass domain rules and inject corruption directly into the ledger.
/// Strictly restricted to the Test projects.
/// </summary>
public class LedgerFaultInjector(CobryxDbContext dbContext)
{
    /// <summary>
    /// Injects a "Partial Commit Failure" where only one leg (debit) of a transaction is saved.
    /// This is the most dangerous scenario in double-entry accounting.
    /// </summary>
    public async Task InjectPartialCommitAsync(Guid tenantId, Guid accountId, decimal amount)
    {
        // Use reflection to create an entry since constructor is internal
        var entry = CreateEntry(tenantId, Guid.NewGuid(), accountId, amount, 0);
        
        dbContext.LedgerEntries.Add(entry);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Injects a transaction that links Tenant A's entry to Tenant B's account.
    /// Simulates multi-tenant data leakage or contamination.
    /// </summary>
    public async Task InjectCrossTenantContaminationAsync(Guid tenantIdA, Guid accountIdB, decimal amount)
    {
        var txId = Guid.NewGuid();
        
        // Tenant A entry pointing to Tenant B's account
        var entryA = CreateEntry(tenantIdA, txId, accountIdB, amount, 0);
        
        dbContext.LedgerEntries.Add(entryA);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Injects a transaction with an imbalanced sum (Σ Debits != Σ Credits).
    /// </summary>
    public async Task InjectImbalanceAsync(Guid tenantId, Guid accountId, decimal debit, decimal credit)
    {
        var entry = CreateEntry(tenantId, Guid.NewGuid(), accountId, debit, credit);
        dbContext.LedgerEntries.Add(entry);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Subtle tampering: Modifies the amount of an EXISTING transaction in DB without updating Hash.
    /// This should be detected as HASH_MISMATCH.
    /// </summary>
    public async Task TamperTransactionAmountAsync(Guid transactionId, decimal newAmount)
    {
        var entry = await dbContext.LedgerEntries.FirstAsync(e => e.TransactionId == transactionId);
        
        SetProperty(entry, "Debit", newAmount);
        
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Swaps Debit/Credit within a Balanced transaction.
    /// Transaction remains balanced (sum=0) but hash must break.
    /// </summary>
    public async Task SwapEntriesAsync(Guid transactionId)
    {
        var entries = await dbContext.LedgerEntries.Where(e => e.TransactionId == transactionId).ToListAsync();
        if (entries.Count < 2) return;

        var d = entries[0].Debit;
        var c = entries[0].Credit;
        
        SetProperty(entries[0], "Debit", entries[1].Debit);
        SetProperty(entries[0], "Credit", entries[1].Credit);
        
        SetProperty(entries[1], "Debit", d);
        SetProperty(entries[1], "Credit", c);

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Changes the AccountId of an entry.
    /// Transaction remains balanced, but hash must break.
    /// </summary>
    public async Task ChangeAccountAsync(Guid transactionId, Guid newAccountId)
    {
        var entry = await dbContext.LedgerEntries.FirstAsync(e => e.TransactionId == transactionId);
        SetProperty(entry, "AccountId", newAccountId);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Breaks the chain by incrementing a sequence ID without updating links.
    /// </summary>
    public async Task BreakChainSequenceAsync(Guid transactionId, long newSequence)
    {
        var tx = await dbContext.LedgerTransactions.FirstAsync(t => t.Id == transactionId);
        SetProperty(tx, "Sequence", newSequence);
        await dbContext.SaveChangesAsync();
    }

    private static LedgerEntry CreateEntry(Guid tenantId, Guid txId, Guid accountId, decimal debit, decimal credit)
    {
        // Accessing internal constructor via reflection
        var entry = (LedgerEntry)Activator.CreateInstance(typeof(LedgerEntry), true)!;
        
        SetProperty(entry, "TenantId", tenantId);
        SetProperty(entry, "TransactionId", txId);
        SetProperty(entry, "AccountId", accountId);
        SetProperty(entry, "Debit", debit);
        SetProperty(entry, "Credit", credit);
        SetProperty(entry, "Currency", "USD");
        
        return entry;
    }

    private static void SetProperty(object target, string name, object value)
    {
        var prop = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(target, value);
    }
}

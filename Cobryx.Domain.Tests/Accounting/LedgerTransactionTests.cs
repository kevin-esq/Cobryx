using Cobryx.Domain.Accounting;
using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Tests.Accounting;

public class LedgerTransactionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _cashAccountId = Guid.NewGuid();
    private readonly Guid _principalAccountId = Guid.NewGuid();

    [Fact]
    public void Post_WhenBalanced_SetsIsPosted()
    {
        var tx = CreateBalancedDraft();
        tx.Post();
        Assert.True(tx.IsPosted);
    }

    [Fact]
    public void Post_WhenUnbalanced_ThrowsJournalUnbalanced()
    {
        var tx = new LedgerTransaction(_tenantId, "Unbalanced", "UB-1");
        tx.AddEntry(_cashAccountId, 100m, 0m);
        tx.AddEntry(_principalAccountId, 0m, 40m);

        var ex = Assert.Throws<DomainException>(() => tx.Post());
        Assert.Equal(DomainErrorCode.Accounting.JournalUnbalanced, ex.ErrorCode);
    }

    [Fact]
    public void AddEntry_AfterPost_ThrowsJournalImmutable()
    {
        var tx = CreateBalancedDraft();
        tx.Post();

        var ex = Assert.Throws<DomainException>(() => tx.AddEntry(_cashAccountId, 1m, 0m));
        Assert.Equal(DomainErrorCode.Accounting.JournalImmutable, ex.ErrorCode);
    }

    [Fact]
    public void AddEntry_AfterSeal_ThrowsJournalImmutable()
    {
        var tx = CreateBalancedDraft();
        tx.Seal("0000000000000000000000000000000000000000000000000000000000000000", 1, "hash");

        var ex = Assert.Throws<DomainException>(() => tx.AddEntry(_cashAccountId, 1m, 0m));
        Assert.Equal(DomainErrorCode.Accounting.JournalImmutable, ex.ErrorCode);
    }

    [Fact]
    public void Seal_WhenUnbalanced_ThrowsJournalUnbalanced()
    {
        var tx = new LedgerTransaction(_tenantId, "Unbalanced seal", "UB-2");
        tx.AddEntry(_cashAccountId, 50m, 0m);
        tx.AddEntry(_principalAccountId, 0m, 40m);

        var ex = Assert.Throws<DomainException>(() =>
            tx.Seal("0000000000000000000000000000000000000000000000000000000000000000", 1, "hash"));

        Assert.Equal(DomainErrorCode.Accounting.JournalUnbalanced, ex.ErrorCode);
    }

    [Fact]
    public void CreateReversal_SwapsDebitAndCredit_AndRemainsBalanced()
    {
        var original = CreateBalancedDraft("ORIG-1");
        original.Post();

        var reversal = LedgerTransaction.CreateReversal(original, "Customer refund");

        Assert.True(reversal.IsReversal);
        Assert.Equal(original.Id, reversal.OriginalTransactionId);
        Assert.True(reversal.IsPosted);

        var originalNet = original.Entries.Sum(e => e.Debit - e.Credit);
        var reversalNet = reversal.Entries.Sum(e => e.Debit - e.Credit);
        Assert.Equal(0m, originalNet);
        Assert.Equal(0m, reversalNet);

        var originalLine = original.Entries.First(e => e.AccountId == _cashAccountId);
        var reversalLine = reversal.Entries.First(e => e.AccountId == _cashAccountId);
        Assert.Equal(originalLine.Debit, reversalLine.Credit);
        Assert.Equal(originalLine.Credit, reversalLine.Debit);
    }

    [Fact]
    public void CreatePartialReversal_WhenAmountExceedsOriginal_Throws()
    {
        var original = CreateBalancedDraft("ORIG-2");
        original.Post();

        var ex = Assert.Throws<DomainException>(() =>
            LedgerTransaction.CreatePartialReversal(original, 200m, "Too much"));

        Assert.Equal(DomainErrorCode.Accounting.ReversalAmountExceeded, ex.ErrorCode);
    }

    [Fact]
    public void CreatePartialReversal_HalfAmount_RemainsBalanced()
    {
        var original = CreateBalancedDraft("ORIG-3");
        original.Post();

        var reversal = LedgerTransaction.CreatePartialReversal(original, 50m, "Partial refund");

        var net = reversal.Entries.Sum(e => e.Debit - e.Credit);
        Assert.Equal(0m, net);
        Assert.True(reversal.IsPosted);
    }

    private LedgerTransaction CreateBalancedDraft(string referenceId = "TX-1")
    {
        var tx = new LedgerTransaction(_tenantId, "Test payment", referenceId);
        tx.AddEntry(_cashAccountId, 100m, 0m);
        tx.AddEntry(_principalAccountId, 0m, 100m);
        return tx;
    }
}

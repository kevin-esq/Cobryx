using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Tests.Accounting;

public class LedgerAccountTests
{
  private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Constructor_NormalizesCurrencyToUppercase()
    {
        var account = new LedgerAccount(
            _tenantId,
            StandardChartOfAccounts.Cash,
            "Cash",
            LedgerAccountType.Asset,
            LedgerAccountRole.Available,
            "mxn",
            isSystem: true);

        Assert.Equal("MXN", account.Currency);
    }

    [Fact]
    public void UpdateDetails_OnSystemAccount_ThrowsSystemAccountLocked()
    {
        var account = new LedgerAccount(
            _tenantId,
            StandardChartOfAccounts.Cash,
            "Cash",
            LedgerAccountType.Asset,
            isSystem: true);

        var ex = Assert.Throws<DomainException>(() => account.UpdateDetails("Renamed", "9999"));
        Assert.Equal(DomainErrorCode.Accounting.SystemAccountLocked, ex.ErrorCode);
    }

    [Fact]
    public void UpdateDetails_OnCustomAccount_AllowsChanges()
    {
        var account = new LedgerAccount(
            _tenantId,
            "6100",
            "Misc",
            LedgerAccountType.Expense,
            isSystem: false);

        account.UpdateDetails("Miscellaneous", "6101");

        Assert.Equal("Miscellaneous", account.Name);
        Assert.Equal("6101", account.Code);
    }

    [Fact]
    public void StandardChartOfAccounts_ContainsSixSystemCodes()
    {
        Assert.Equal(6, StandardChartOfAccounts.AllSystemCodes.Length);
        Assert.Equal(StandardChartOfAccounts.Cash, StandardChartOfAccounts.AllSystemCodes[0]);
    }

    [Fact]
    public void StandardChartOfAccounts_DescribeCash_ReturnsAssetAvailable()
    {
        var (code, name, type, role) = StandardChartOfAccounts.Describe(StandardChartOfAccounts.Cash);
        Assert.Equal(StandardChartOfAccounts.Cash, code);
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(LedgerAccountType.Asset, type);
        Assert.Equal(LedgerAccountRole.Available, role);
    }
}

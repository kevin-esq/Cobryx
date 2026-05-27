using Cobryx.Domain.Accounting.Enums;

namespace Cobryx.Domain.Accounting;

/// <summary>
/// Stable account codes for per-tenant system chart of accounts (ADR-008).
/// </summary>
public static class StandardChartOfAccounts
{
    public const string Cash = "1010";
    public const string PrincipalReceivable = "1210";
    public const string InterestIncome = "4010";
    public const string FeeRevenue = "4020";
    public const string LossExpense = "5010";
    public const string RecoveryIncome = "4030";

    public static readonly string[] AllSystemCodes =
    [
        Cash,
        PrincipalReceivable,
        InterestIncome,
        FeeRevenue,
        LossExpense,
        RecoveryIncome
    ];

    public static (string Code, string Name, LedgerAccountType Type, LedgerAccountRole Role) Describe(string code) =>
        code switch
        {
            Cash => (Cash, "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available),
            PrincipalReceivable => (PrincipalReceivable, "Principal Receivable", LedgerAccountType.Asset, LedgerAccountRole.Receivable),
            InterestIncome => (InterestIncome, "Interest Income", LedgerAccountType.Revenue, LedgerAccountRole.None),
            FeeRevenue => (FeeRevenue, "Fee Revenue", LedgerAccountType.Revenue, LedgerAccountRole.Fees),
            LossExpense => (LossExpense, "Loss Expense", LedgerAccountType.Expense, LedgerAccountRole.Loss),
            RecoveryIncome => (RecoveryIncome, "Recovery Income", LedgerAccountType.Revenue, LedgerAccountRole.None),
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Not a standard system account code.")
        };
}

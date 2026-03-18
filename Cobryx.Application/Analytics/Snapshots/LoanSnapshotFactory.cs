using Cobryx.Domain.Analytics;
using Cobryx.Domain.Lending;

namespace Cobryx.Application.Analytics.Snapshots;

public static class LoanSnapshotFactory
{
    public static LoanBalanceSnapshot Create(
        Loan loan,
        SnapshotType type,
        long ledgerSequenceId)
    {
        return new LoanBalanceSnapshot(
            loan.TenantId,
            loan.Id,
            type,
            loan.CurrentPrincipalBalance,
            loan.CurrentInterestBalance,
            loan.CurrentLateFeeBalance,
            loan.DaysInArrears,
            ledgerSequenceId,
            DateTime.UtcNow
        );
    }
}

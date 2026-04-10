using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Lending;

namespace Cobryx.Application.Analytics.Snapshots
{
    public interface ILoanSnapshotFactory
    {
        public LoanBalanceSnapshot Create(Loan loan, SnapshotType type, long ledgerSequenceId);
    }

    public class LoanSnapshotFactory(IClock clock) : ILoanSnapshotFactory
    {
        public LoanBalanceSnapshot Create(
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
                clock.UtcNow
            );
        }
    }
}

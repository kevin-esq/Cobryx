using Cobryx.Application.Common.Events;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Events.Lending;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Analytics.Snapshots.EventHandlers
{
    public class LoanDisbursedSnapshotHandler(
        IUnitOfWork unitOfWork,
        ILoanSnapshotFactory snapshotFactory) : INotificationHandler<DomainEventNotification<LoanDisbursedEvent>>
    {
        public async Task Handle(DomainEventNotification<LoanDisbursedEvent> notification,
            CancellationToken cancellationToken)
        {
            LoanDisbursedEvent evt = notification.DomainEvent;
            var dbContext = (DbContext)unitOfWork;

            Loan? loan = await dbContext.Set<Loan>()
                .FirstOrDefaultAsync(x => x.Id == evt.LoanId, cancellationToken);
            if (loan == null)
            {
                return;
            }

            var sequenceId = evt.LedgerSequenceId > 0 ? evt.LedgerSequenceId : evt.OccurredOn.Ticks;

            LoanBalanceSnapshot snapshot = snapshotFactory.Create(loan, SnapshotType.Disbursement, sequenceId);
            _ = dbContext.Set<LoanBalanceSnapshot>().Add(snapshot);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

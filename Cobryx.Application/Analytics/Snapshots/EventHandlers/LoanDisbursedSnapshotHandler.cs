using Cobryx.Application.Common.Events;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Events.Lending;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Analytics.Snapshots.EventHandlers;

public class LoanDisbursedSnapshotHandler(IUnitOfWork unitOfWork) : INotificationHandler<DomainEventNotification<LoanDisbursedEvent>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task Handle(DomainEventNotification<LoanDisbursedEvent> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        var dbContext = (DbContext)_unitOfWork;

        var loan = await dbContext.Set<Domain.Lending.Loan>().FirstOrDefaultAsync(x => x.Id == evt.LoanId, ct);
        if (loan == null)
            return;

        var sequenceId = evt.LedgerSequenceId > 0 ? evt.LedgerSequenceId : evt.OccurredOn.Ticks;

        var snapshot = LoanSnapshotFactory.Create(loan, SnapshotType.Disbursement, sequenceId);
        dbContext.Set<LoanBalanceSnapshot>().Add(snapshot);
        await dbContext.SaveChangesAsync(ct);
    }
}

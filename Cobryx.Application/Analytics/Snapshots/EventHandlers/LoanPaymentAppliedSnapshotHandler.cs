using Cobryx.Application.Common.Events;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Events.Lending;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Analytics.Snapshots.EventHandlers;

public class LoanPaymentAppliedSnapshotHandler : INotificationHandler<DomainEventNotification<LoanPaymentAppliedEvent>>
{
    private readonly IUnitOfWork _unitOfWork;

    public LoanPaymentAppliedSnapshotHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<LoanPaymentAppliedEvent> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        var dbContext = (DbContext)_unitOfWork;

        var loan = await dbContext.Set<Cobryx.Domain.Lending.Loan>().FirstOrDefaultAsync(x => x.Id == evt.LoanId, ct);
        if (loan == null)
            return;

        var sequenceId = evt.LedgerSequenceId > 0 ? evt.LedgerSequenceId : evt.OccurredOn.Ticks;

        var snapshot = LoanSnapshotFactory.Create(loan, SnapshotType.Payment, sequenceId);
        dbContext.Set<LoanBalanceSnapshot>().Add(snapshot);
        await dbContext.SaveChangesAsync(ct);
    }
}

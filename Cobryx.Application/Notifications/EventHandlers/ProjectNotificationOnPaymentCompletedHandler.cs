using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Notifications.EventHandlers;

public class ProjectNotificationOnPaymentCompletedHandler : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectNotificationOnPaymentCompletedHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var dbContext = (DbContext)_unitOfWork;

        var exists = await dbContext.Set<Notification>()
            .AnyAsync(n => n.TenantId == domainEvent.TenantId &&
                         n.RelatedEntityId == domainEvent.PaymentId.ToString() &&
                         n.Type == NotificationType.Success, cancellationToken);

        if (exists) return;

        var userNotification = new Notification(
            domainEvent.TenantId,
            "Payment Received",
            $"A payment of {domainEvent.Amount.Amount} {domainEvent.Amount.Currency} has been successfully processed.",
            NotificationType.Success,
            relatedEntityId: domainEvent.PaymentId.ToString(),
            relatedEntityType: "Payment"
        );

        dbContext.Set<Notification>().Add(userNotification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

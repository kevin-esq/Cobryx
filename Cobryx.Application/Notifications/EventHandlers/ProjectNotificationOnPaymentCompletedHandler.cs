using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;

using Concordia;

namespace Cobryx.Application.Notifications.EventHandlers
{
    public class ProjectNotificationOnPaymentCompletedHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork) : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
    {
        public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification,
            CancellationToken cancellationToken)
        {
            PaymentCompletedEvent domainEvent = notification.DomainEvent;

            var exists = await notificationRepository.ExistsAsync(
                domainEvent.TenantId,
                domainEvent.PaymentId.ToString(),
                NotificationType.Success,
                cancellationToken);

            if (exists)
            {
                return;
            }

            Notification userNotification = new(
                domainEvent.TenantId,
                "Payment Received",
                $"A payment of {domainEvent.Amount.Amount} {domainEvent.Amount.Currency} has been successfully processed.",
                NotificationType.Success,
                relatedEntityId: domainEvent.PaymentId.ToString(),
                relatedEntityType: "Payment");

            await notificationRepository.AddAsync(userNotification, cancellationToken);
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;

using Concordia;

namespace Cobryx.Application.Notifications.EventHandlers
{
    public class ProjectNotificationOnUserRegisteredHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork) : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
    {
        public async Task Handle(DomainEventNotification<UserRegisteredEvent> notification,
            CancellationToken cancellationToken)
        {
            UserRegisteredEvent domainEvent = notification.DomainEvent;

            var exists = await notificationRepository.ExistsAsync(
                domainEvent.TenantId,
                domainEvent.UserId.ToString(),
                NotificationType.Information,
                cancellationToken);

            if (exists)
            {
                return;
            }

            Notification userNotification = new(
                domainEvent.TenantId,
                "New User Registered",
                $"A new user with email {domainEvent.Email} has joined your organization.",
                userId: domainEvent.UserId,
                relatedEntityId: domainEvent.UserId.ToString(),
                relatedEntityType: "User");

            await notificationRepository.AddAsync(userNotification, cancellationToken);
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

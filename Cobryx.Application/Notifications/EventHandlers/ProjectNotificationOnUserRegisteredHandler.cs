using Cobryx.Domain.Entities;
using Cobryx.Domain.Events;
using Cobryx.Application.Common.Events;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Notifications.EventHandlers;

public class ProjectNotificationOnUserRegisteredHandler : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectNotificationOnUserRegisteredHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<UserRegisteredEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var dbContext = (DbContext)_unitOfWork;

        var exists = await dbContext.Set<Notification>()
            .AnyAsync(n => n.TenantId == domainEvent.TenantId &&
                         n.RelatedEntityId == domainEvent.UserId.ToString() &&
                         n.Type == NotificationType.Information, cancellationToken);

        if (exists) return;

        var userNotification = new Notification(
            domainEvent.TenantId,
            "New User Registered",
            $"A new user with email {domainEvent.Email} has joined your organization.",
            NotificationType.Information,
            userId: domainEvent.UserId,
            relatedEntityId: domainEvent.UserId.ToString(),
            relatedEntityType: "User"
        );

        dbContext.Set<Notification>().Add(userNotification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

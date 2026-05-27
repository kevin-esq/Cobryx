using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Notifications.Commands.MarkAllNotificationsRead
{
    [TenantScoped]
public record MarkAllNotificationsReadCommand : IRequest<Result>, IRequiresTenant;

    public class MarkAllNotificationsReadHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider) : IRequestHandler<MarkAllNotificationsReadCommand, Result>
    {
        public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure(DomainErrorCode.Tenant.ContextMissing);
            }

            List<Notification> notifications = await notificationRepository
                .GetUnreadByTenantAsync(tenantId.Value, cancellationToken);

            foreach (Notification notification in notifications)
            {
                notification.MarkAsRead();
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Notifications.Commands.MarkNotificationRead
{
    public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

    public class MarkNotificationReadHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider) : IRequestHandler<MarkNotificationReadCommand, Result>
    {
        public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure(DomainErrorCode.Tenant.ContextMissing);
            }

            Domain.Identity.Notification? notification = await notificationRepository
                .GetByIdForTenantAsync(request.NotificationId, tenantId.Value, cancellationToken);

            if (notification == null)
            {
                return Result.Failure(DomainErrorCode.Common.EntityNotFound);
            }

            notification.MarkAsRead();
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

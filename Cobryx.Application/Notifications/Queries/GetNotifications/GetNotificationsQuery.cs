using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Notifications.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Notifications.Queries.GetNotifications
{
    [TenantScoped]
public record GetNotificationsQuery(bool UnreadOnly = true, int Limit = 20) : IRequest<Result<List<NotificationDto>>>, IRequiresTenant;

    public class GetNotificationsHandler(
        INotificationRepository notificationRepository,
        ITenantProvider tenantProvider) : IRequestHandler<GetNotificationsQuery, Result<List<NotificationDto>>>
    {
        public async Task<Result<List<NotificationDto>>> Handle(
            GetNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<List<NotificationDto>>(DomainErrorCode.Tenant.ContextMissing);
            }

            List<Notification> notifications = await notificationRepository.GetByTenantAsync(
                tenantId.Value,
                request.UnreadOnly,
                request.Limit,
                cancellationToken);

            List<NotificationDto> dtos =
            [
                .. notifications.Select(n => new NotificationDto(
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type.ToString(),
                    n.IsRead,
                    n.CreatedAt))
            ];

            return Result.Success(dtos);
        }
    }
}

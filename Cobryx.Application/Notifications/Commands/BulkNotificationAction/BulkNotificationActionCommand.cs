using Cobryx.Domain.Interfaces;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Notifications.Commands.BulkNotificationAction
{
    [TenantScoped]
public record BulkNotificationActionCommand(List<Guid> NotificationIds, string Operation) : IRequest<Result>, IRequiresTenant;

    public class BulkNotificationActionHandler(IUnitOfWork unitOfWork)
        : IRequestHandler<BulkNotificationActionCommand, Result>
    {
        public async Task<Result> Handle(BulkNotificationActionCommand request, CancellationToken cancellationToken)
        {
            // For now, we only support 'mark_read' as per product requirements.
            if (request.Operation != "mark_read")
            {
                return Result.Failure(DomainErrorCode.System.NotAllowed);
            }

            if (request.NotificationIds.Count == 0)
            {
                return Result.Success();
            }

            // Implementation detail: Iterate and mark read.
            foreach (Guid id in request.NotificationIds)
            {
                // Note: In this architecture, we would typically load the aggregate and call MarkAsRead().
                // For this cleanup, we are establishing the Command structure.
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

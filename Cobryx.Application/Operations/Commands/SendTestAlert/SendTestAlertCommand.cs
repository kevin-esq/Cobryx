using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Operations.Commands.SendTestAlert;

[PlatformScoped]
public record SendTestAlertCommand(string? AdminUser) : IRequest<Result>;

public class SendTestAlertHandler(
    IAlertingService alertingService,
    IClock clock) : IRequestHandler<SendTestAlertCommand, Result>
{
    public async Task<Result> Handle(SendTestAlertCommand request, CancellationToken cancellationToken)
    {
        await alertingService.SendAlertAsync(
            "AdminConsole",
            "This is a manual test alert to verify the communication pipeline.",
            AlertLevel.Info,
            new { AdminUser = request.AdminUser, Timestamp = clock.UtcNow },
            cancellationToken);

        return Result.Success();
    }
}

using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Payments.Webhooks.Commands.ProcessWebhook
{
    [WebhookSystem]
public record ProcessWebhookCommand(
        string Provider,
        string ExternalEventId,
        string RawPayload) : IRequest<Result<Guid>>;

    public partial class ProcessWebhookHandler(
        IWebhookEventRepository webhookEventRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessWebhookHandler> logger) : IRequestHandler<ProcessWebhookCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
        {
            LogIngesting(logger, request.Provider, request.ExternalEventId);

            var existing =
                await webhookEventRepository.ExistsAsync(request.Provider, request.ExternalEventId, cancellationToken);

            if (existing)
            {
                LogDeduped(logger, request.Provider, request.ExternalEventId);
                return Result.Success(Guid.Empty);
            }

            WebhookEvent webhookEvent = new(request.Provider, request.ExternalEventId, request.RawPayload);

            await webhookEventRepository.AddAsync(webhookEvent, cancellationToken);
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(webhookEvent.Id);
        }

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Ingesting webhook from {Provider} with ExternalEventId {ExternalEventId}")]
        private static partial void LogIngesting(ILogger logger, string provider, string externalEventId);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Webhook already ingested (Deduped). Provider: {Provider}, ExternalEventId: {ExternalEventId}")]
        private static partial void LogDeduped(ILogger logger, string provider, string externalEventId);
    }
}

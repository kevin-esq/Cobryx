using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Webhooks.Commands.ProcessWebhook;

public record ProcessWebhookCommand(
    string Provider,
    string ExternalEventId,
    string RawPayload) : IRequest<Result>;

public class ProcessWebhookHandler : IRequestHandler<ProcessWebhookCommand, Result>
{
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessWebhookHandler> _logger;

    public ProcessWebhookHandler(
        IWebhookEventRepository webhookEventRepository, 
        IUnitOfWork unitOfWork,
        ILogger<ProcessWebhookHandler> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting webhook from {Provider} with ExternalEventId {ExternalEventId}", 
            request.Provider, request.ExternalEventId);

        var existing = await _webhookEventRepository.ExistsAsync(request.Provider, request.ExternalEventId, cancellationToken);

        if (existing)
        {
            _logger.LogInformation("Webhook already ingested (Deduped). Provider: {Provider}, ExternalEventId: {ExternalEventId}", 
                request.Provider, request.ExternalEventId);
            return Result.Success();
        }

        var webhookEvent = new WebhookEvent(request.Provider, request.ExternalEventId, request.RawPayload);
        
        await _webhookEventRepository.AddAsync(webhookEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

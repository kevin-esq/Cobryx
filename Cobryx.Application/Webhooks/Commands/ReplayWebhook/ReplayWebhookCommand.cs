using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Payments.Webhooks.Commands.HandleWebhookEvent;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Webhooks.Commands.ReplayWebhook
{
    /// <summary>
    /// Command to re-trigger a webhook processing flow.
    /// </summary>
    /// <param name="WebhookEventId">Internal ID of the webhook event.</param>
    /// <param name="IsForced">If true, bypasses the processed check by removing the idempotency record.</param>
    /// <param name="Reason">Mandatory reason for the replay (especially for forced replays).</param>
    /// <param name="IsDryRun">If true, simulates the replay and returns expected side effects without committing.</param>
    [TenantScoped]
    public record ReplayWebhookCommand(
        Guid WebhookEventId,
        bool IsForced = false,
        string? Reason = null,
        bool IsDryRun = false) : IRequest<Result>, IRequiresTenant;

    public class ReplayWebhookHandler(
        IWebhookEventRepository webhookEventRepository,
        IProcessedWebhookEventRepository processedEventRepository,
        ISecurityAuditService auditService,
        ICurrentUserProvider currentUserProvider,
        ITenantProvider tenantProvider,
        IHttpContextService httpContextService,
        ISender sender,
        IUnitOfWork unitOfWork,
        ILogger<ReplayWebhookHandler> logger)
        : IRequestHandler<ReplayWebhookCommand, Result>
    {
        public async Task<Result> Handle(ReplayWebhookCommand request, CancellationToken cancellationToken)
        {
            if (request.IsForced && string.IsNullOrWhiteSpace(request.Reason))
            {
                return Result.Failure(DomainErrorCode.Common.ReasonRequired);
            }

            WebhookEvent? webhookEvent =
                await webhookEventRepository.GetByIdAsync(request.WebhookEventId, cancellationToken);
            if (webhookEvent == null)
            {
                return Result.Failure(DomainErrorCode.Webhooks.EventNotFound);
            }

            var userId = currentUserProvider.GetUserId()?.ToString();
            Guid? tenantId = tenantProvider.GetTenantId();
            var ip = httpContextService.GetIpAddress();

            // 1. Audit the intent
            var auditMetadata = new
            {
                request.WebhookEventId,
                webhookEvent.Provider,
                webhookEvent.ExternalEventId,
                TenantId = tenantId,
                request.IsForced,
                request.IsDryRun,
                request.Reason
            };

            if (request.IsDryRun)
            {
                logger.LogInformation("Webhook Replay SIMULATED (Dry Run) for Event {WebhookEventId} by User {UserId}",
                    request.WebhookEventId, userId);

                auditService.LogSuccess("WEBHOOK.REPLAY_SIMULATED", userId, ip, auditMetadata);
                // In dry run, we just return Success with a simulated outcome
                return Result.Success();
            }

            if (request.IsForced)
            {
                logger.LogWarning(
                    "Forced replay executed for Webhook {WebhookId} by User {UserId} in Tenant {TenantId}. Reason: {Reason}",
                    request.WebhookEventId,
                    userId,
                    tenantId,
                    request.Reason ?? "No reason provided");

                auditService.LogCritical("WEBHOOK.FORCED_REPLAY", userId, ip, request.Reason!,
                    auditMetadata);
            }
            else
            {
                logger.LogInformation(
                    "Standard replay executed for Webhook {WebhookId} by User {UserId} in Tenant {TenantId}",
                    request.WebhookEventId,
                    userId,
                    tenantId);

                auditService.LogSuccess("WEBHOOK.REPLAY", userId, ip, auditMetadata);
            }

            // 2. Reset the event status
            webhookEvent.ResetToPending();
            await webhookEventRepository.UpdateAsync(webhookEvent, cancellationToken);

            // 3. If forced, remove the domain-level idempotency record
            if (request.IsForced)
            {
                await processedEventRepository.RemoveAsync(webhookEvent.Provider, webhookEvent.ExternalEventId,
                    cancellationToken);
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Re-trigger the handler immediately for better UX
            return await sender.Send(new HandleWebhookEventCommand(request.WebhookEventId), cancellationToken);
        }
    }
}

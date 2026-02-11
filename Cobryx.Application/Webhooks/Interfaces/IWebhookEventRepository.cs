using Cobryx.Application.Webhooks.Entities;

namespace Cobryx.Application.Webhooks.Interfaces;

public interface IWebhookEventRepository
{
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default);
}

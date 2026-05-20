using Cobryx.Application.Webhooks.Entities;

namespace Cobryx.Application.Webhooks.Interfaces
{
    public interface IWebhookEventRepository
    {
        public Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
        public Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
        public Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        public Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default);
        public Task<IEnumerable<WebhookEvent>> GetRecentAsync(
            WebhookStatus? status = null,
            int limit = 100,
            int offset = 0,
            DateTime? since = null,
            CancellationToken cancellationToken = default);
    }
}

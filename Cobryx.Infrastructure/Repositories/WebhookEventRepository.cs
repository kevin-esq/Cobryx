using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories
{
    public class WebhookEventRepository(CobryxDbContext dbContext) : IWebhookEventRepository
    {
        private readonly CobryxDbContext _dbContext = dbContext;

        public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default) => _ = await _dbContext.WebhookEvents.AddAsync(webhookEvent, cancellationToken);

        public async Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
        {
            _ = _dbContext.WebhookEvents.Update(webhookEvent);
            await Task.CompletedTask;
        }

        public async Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => await _dbContext.WebhookEvents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        public async Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default) => await _dbContext.WebhookEvents.AnyAsync(e => e.Provider == provider && e.ExternalEventId == externalEventId, cancellationToken);

        public async Task<IEnumerable<WebhookEvent>> GetRecentAsync(
            WebhookStatus? status = null,
            int limit = 100,
            int offset = 0,
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.WebhookEvents.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            if (since.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= since.Value);
            }

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}

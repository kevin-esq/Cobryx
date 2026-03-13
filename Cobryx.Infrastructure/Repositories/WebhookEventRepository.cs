using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly CobryxDbContext _dbContext;

    public WebhookEventRepository(CobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.WebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }

    public async Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.WebhookEvents.Update(webhookEvent);
        await Task.CompletedTask;
    }

    public async Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WebhookEvents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WebhookEvents.AnyAsync(e => e.Provider == provider && e.ExternalEventId == externalEventId, cancellationToken);
    }
}

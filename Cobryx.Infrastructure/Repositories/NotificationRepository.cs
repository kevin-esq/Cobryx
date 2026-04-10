using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="INotificationRepository"/>.
    /// </summary>
    public class NotificationRepository(CobryxDbContext dbContext) : INotificationRepository
    {
        public async Task<Notification?> GetByIdForTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        {
            return await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId, ct);
        }

        public async Task<List<Notification>> GetByTenantAsync(
            Guid tenantId,
            bool unreadOnly,
            int limit,
            CancellationToken ct = default)
        {
            IQueryable<Notification> query = dbContext.Notifications
                .Where(n => n.TenantId == tenantId && !n.IsDeleted);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Notification>> GetUnreadByTenantAsync(Guid tenantId, CancellationToken ct = default)
        {
            return await dbContext.Notifications
                .Where(n => n.TenantId == tenantId && !n.IsRead)
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsAsync(
            Guid tenantId,
            string relatedEntityId,
            NotificationType type,
            CancellationToken ct = default)
        {
            return await dbContext.Notifications
                .AnyAsync(n => n.TenantId == tenantId &&
                               n.RelatedEntityId == relatedEntityId &&
                               n.Type == type, ct);
        }

        public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
            _ = await dbContext.Notifications.AddAsync(notification, ct);
    }
}

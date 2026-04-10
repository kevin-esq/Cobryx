using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces
{
    /// <summary>
    /// Persistence contract for <see cref="Notification"/> entities.
    /// Notification is not an aggregate root, so this interface is standalone.
    /// </summary>
    public interface INotificationRepository
    {
        public Task<Notification?> GetByIdForTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default);

        public Task<List<Notification>> GetByTenantAsync(
            Guid tenantId,
            bool unreadOnly,
            int limit,
            CancellationToken ct = default);

        public Task<List<Notification>> GetUnreadByTenantAsync(Guid tenantId, CancellationToken ct = default);

        public Task<bool> ExistsAsync(Guid tenantId, string relatedEntityId, NotificationType type,
            CancellationToken ct = default);

        public Task AddAsync(Notification notification, CancellationToken ct = default);
    }
}

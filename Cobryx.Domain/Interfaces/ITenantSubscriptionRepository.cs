using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces;

public interface ITenantSubscriptionRepository : IRepository<TenantSubscription>
{
    public Task<TenantSubscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    public Task<TenantSubscription?> GetByTenantIdWithLockAsync(Guid tenantId, CancellationToken ct = default);
}

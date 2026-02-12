using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ITenantSubscriptionRepository : IRepository<TenantSubscription>
{
    Task<TenantSubscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}

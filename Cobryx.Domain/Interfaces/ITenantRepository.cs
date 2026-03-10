using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByStripeAccountIdAsync(string stripeAccountId, CancellationToken ct = default);
}

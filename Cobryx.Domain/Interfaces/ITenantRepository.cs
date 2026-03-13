using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    public Task<Tenant?> GetByStripeAccountIdAsync(string stripeAccountId, CancellationToken cancellationToken = default);
}

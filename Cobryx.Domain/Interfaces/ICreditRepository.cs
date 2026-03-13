using Cobryx.Domain.Lending;

namespace Cobryx.Domain.Interfaces;

public interface ICreditRepository : IRepository<Credit>
{
    public Task<IEnumerable<Credit>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    public Task<IEnumerable<Credit>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ICreditRepository : IRepository<Credit>
{
    Task<IEnumerable<Credit>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credit>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

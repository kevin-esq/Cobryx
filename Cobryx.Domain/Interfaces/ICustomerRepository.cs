using Cobryx.Domain.Lending;

namespace Cobryx.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    public Task<Customer?> GetByPhoneAsync(Guid tenantId, string phone, CancellationToken cancellationToken = default);
    public Task<IEnumerable<Customer>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

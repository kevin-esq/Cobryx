using Cobryx.Domain.Accounting;


namespace Cobryx.Domain.Interfaces;

public interface ITaxConfigurationRepository : IRepository<TaxConfiguration>
{
    public Task<TaxConfiguration?> GetDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TaxConfiguration>> GetAllActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

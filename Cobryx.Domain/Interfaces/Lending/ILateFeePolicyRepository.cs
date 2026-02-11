using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

public interface ILateFeePolicyRepository
{
    Task<LateFeePolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LateFeePolicy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(LateFeePolicy policy, CancellationToken ct = default);
    Task UpdateAsync(LateFeePolicy policy, CancellationToken ct = default);
}

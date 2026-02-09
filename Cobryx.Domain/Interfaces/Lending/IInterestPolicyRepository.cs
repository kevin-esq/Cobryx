using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

public interface IInterestPolicyRepository
{
    Task<InterestPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<InterestPolicy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(InterestPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(InterestPolicy policy, CancellationToken ct = default);
}

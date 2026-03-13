namespace Cobryx.Domain.Lending;

public interface ILateFeePolicyRepository
{
    public Task<LateFeePolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<IReadOnlyList<LateFeePolicy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    public Task AddAsync(LateFeePolicy policy, CancellationToken ct = default);
    public Task UpdateAsync(LateFeePolicy policy, CancellationToken ct = default);
}

namespace Cobryx.Domain.Lending;

public interface IInterestPolicyRepository
{
    public Task<InterestPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<InterestPolicy?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default);
    public Task<IReadOnlyList<InterestPolicy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    public Task AddAsync(InterestPolicy policy, CancellationToken ct = default);
    public Task UpdateAsync(InterestPolicy policy, CancellationToken ct = default);
}

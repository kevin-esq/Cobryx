namespace Cobryx.Domain.Lending;

public interface IPaymentApplicationPolicyRepository
{
    public Task<PaymentApplicationPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<PaymentApplicationPolicy?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default);
    public Task<PaymentApplicationPolicy?> GetDefaultByTenantAsync(Guid tenantId, CancellationToken ct = default);
    public Task<IReadOnlyList<PaymentApplicationPolicy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    public Task AddAsync(PaymentApplicationPolicy policy, CancellationToken ct = default);
    public Task UpdateAsync(PaymentApplicationPolicy policy, CancellationToken ct = default);
}

using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

public interface IPaymentApplicationPolicyRepository
{
    Task<PaymentApplicationPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentApplicationPolicy?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default);
    Task<PaymentApplicationPolicy?> GetDefaultByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentApplicationPolicy>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(PaymentApplicationPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(PaymentApplicationPolicy policy, CancellationToken ct = default);
}

using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories.Lending;

/// <summary>
/// Consolidated repository for managing lending policy persistence using explicit interface implementation.
/// </summary>
public class PolicyRepository : IInterestPolicyRepository, ILateFeePolicyRepository, IPaymentApplicationPolicyRepository
{
    private readonly CobryxDbContext _context;

    public PolicyRepository(CobryxDbContext context)
    {
        _context = context;
    }

    // IInterestPolicyRepository
    async Task<InterestPolicy?> IInterestPolicyRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.InterestPolicies.FindAsync(new object[] { id }, ct);

    async Task<IReadOnlyList<InterestPolicy>> IInterestPolicyRepository.GetActiveByTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _context.InterestPolicies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync(ct);

    async Task IInterestPolicyRepository.AddAsync(InterestPolicy policy, CancellationToken ct) =>
        await _context.InterestPolicies.AddAsync(policy, ct);

    async Task IInterestPolicyRepository.UpdateAsync(InterestPolicy policy, CancellationToken ct)
    {
        _context.InterestPolicies.Update(policy);
        await Task.CompletedTask;
    }

    // ILateFeePolicyRepository
    async Task<LateFeePolicy?> ILateFeePolicyRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.LateFeePolicies.FindAsync(new object[] { id }, ct);

    async Task<IReadOnlyList<LateFeePolicy>> ILateFeePolicyRepository.GetActiveByTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _context.LateFeePolicies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync(ct);

    async Task ILateFeePolicyRepository.AddAsync(LateFeePolicy policy, CancellationToken ct) =>
        await _context.LateFeePolicies.AddAsync(policy, ct);

    async Task ILateFeePolicyRepository.UpdateAsync(LateFeePolicy policy, CancellationToken ct)
    {
        _context.LateFeePolicies.Update(policy);
        await Task.CompletedTask;
    }

    // IPaymentApplicationPolicyRepository
    async Task<PaymentApplicationPolicy?> IPaymentApplicationPolicyRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.PaymentApplicationPolicies.FindAsync(new object[] { id }, ct);

    async Task<PaymentApplicationPolicy?> IPaymentApplicationPolicyRepository.GetDefaultByTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _context.PaymentApplicationPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault && p.IsActive, ct);

    async Task<IReadOnlyList<PaymentApplicationPolicy>> IPaymentApplicationPolicyRepository.GetActiveByTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _context.PaymentApplicationPolicies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync(ct);

    async Task IPaymentApplicationPolicyRepository.AddAsync(PaymentApplicationPolicy policy, CancellationToken ct) =>
        await _context.PaymentApplicationPolicies.AddAsync(policy, ct);

    async Task IPaymentApplicationPolicyRepository.UpdateAsync(PaymentApplicationPolicy policy, CancellationToken ct)
    {
        _context.PaymentApplicationPolicies.Update(policy);
        await Task.CompletedTask;
    }
}

namespace Cobryx.Application.Common.Interfaces;

public interface ISubscriptionEnforcementService
{
    public Task EnsureWithinInvoicesLimitAsync(Guid tenantId, CancellationToken ct = default);
    public Task EnsureWithinUsersLimitAsync(Guid tenantId, CancellationToken ct = default);
    public Task EnsureWithinLoansLimitAsync(Guid tenantId, CancellationToken ct = default);
    public Task EnsureSubscriptionActiveAsync(Guid tenantId, CancellationToken ct = default);
}

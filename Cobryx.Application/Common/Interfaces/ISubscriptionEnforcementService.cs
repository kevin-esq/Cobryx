using Cobryx.Domain.Enums;

namespace Cobryx.Application.Common.Interfaces;

public interface ISubscriptionEnforcementService
{
    Task EnsureWithinInvoicesLimitAsync(Guid tenantId, CancellationToken ct = default);
    Task EnsureWithinUsersLimitAsync(Guid tenantId, CancellationToken ct = default);
    Task EnsureWithinLoansLimitAsync(Guid tenantId, CancellationToken ct = default);
    Task EnsureSubscriptionActiveAsync(Guid tenantId, CancellationToken ct = default);
}

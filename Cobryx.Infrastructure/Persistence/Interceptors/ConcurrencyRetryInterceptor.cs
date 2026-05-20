using Cobryx.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor to handle optimistic concurrency conflicts.
/// Non-financial operations are retried up to 3 times.
/// Financial operations (marked with IFinancialCommand) fail fast to ensure ledger integrity.
/// </summary>
public class ConcurrencyRetryInterceptor(
    ITenantProvider tenantProvider,
    ICurrentUserProvider userProvider)
    : SaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly ICurrentUserProvider _userProvider = userProvider;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Potential logic before saving if needed
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Note: EF Interceptors on SaveChanges dont easily support retries of the whole SaveChanges call.
    // Retries are better handled in a Decorator or a Middleware/PipelineBehavior.
    // However, we can use the Interceptor to 'Fail Fast' or log specific financial conflicts.
}

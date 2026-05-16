using Cobryx.Application.Common.Exceptions;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Models;
using Cobryx.Infrastructure.Persistence;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.PipelineBehaviors
{
    /// <summary>
    /// Pipeline behavior to handle optimistic concurrency conflicts.
    /// Non-financial operations are retried up to 3 times with exponential backoff.
    /// Financial operations (IFinancialCommand) fail fast to prevent double-spending.
    /// Also enforces the Financial Safe Mode lockdown.
    /// </summary>
    public class ConcurrencyBehavior<TRequest, TResponse>(
        ILedgerHealthCache healthCache,
        ITenantProvider tenantProvider,
        CobryxDbContext dbContext,
        ILogger<ConcurrencyBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const int MaxRetries = 3;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var isFinancial = request is IFinancialCommand;
            var requestName = typeof(TRequest).Name;

            // 1. ELITE SAFETY CHECK: Total Financial Lockdown Wall
            if (isFinancial)
            {
                var tenantId = tenantProvider.GetTenantId();
                if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                {
                    LedgerHealthStatus status = healthCache.Get(tenantId.Value);
                    if (status.IsSafeMode)
                    {
                        logger.LogCritical("BLOCKING FINANCIAL OPERATION: Tenant {TenantId} is in Safe Mode. Reason: {Reason}. Triggered At: {TriggeredAt}",
                            tenantId.Value, status.Reason, status.TriggeredAt);

                        throw new FinancialSafeModeException(tenantId.Value, status.Reason);
                    }
                }
            }

            var retryCount = 0;

            while (true)
            {
                try
                {
                    return await next(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (isFinancial)
                    {
                        logger.LogCritical(ex,
                            "CONCURRENCY FATAL: Financial operation {RequestName} failed due to version conflict. Failing fast to protect ledger integrity.",
                            requestName);
                        throw;
                    }

                    retryCount++;
                    if (retryCount > MaxRetries)
                    {
                        logger.LogError(ex,
                            "CONCURRENCY FAILURE: Non-financial operation {RequestName} failed after {RetryCount} retries.",
                            requestName, MaxRetries);
                        throw;
                    }

                    logger.LogWarning(ex,
                        "CONCURRENCY RETRY: Non-financial operation {RequestName} failed (Attempt {RetryCount}/{MaxCount}). Retrying with exponential backoff...",
                        requestName, retryCount, MaxRetries);

                    // Stale optimistic concurrency tokens (Version) stay in the change tracker; a blind retry
                    // would keep incrementing in-memory Version while the DB row never matched.
                    dbContext.ChangeTracker.Clear();

                    // Exponential backoff: 100ms, 200ms, 400ms
                    await Task.Delay(100 * (int)Math.Pow(2, retryCount - 1), cancellationToken);
                }
            }
        }
    }
}

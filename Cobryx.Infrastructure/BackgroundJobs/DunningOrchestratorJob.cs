using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Systematic heartbeat for the Revenue Engine 2.0.
/// Scans for pending recoveries and triggers the Orchestration Engine while respecting 
/// dunning intervals, max horizons, and concurrency guards.
/// </summary>
public class DunningOrchestratorJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentOrchestrationService _orchestrationService;
    private readonly IClock _clock;
    private readonly ILogger<DunningOrchestratorJob> _logger;

    public DunningOrchestratorJob(
        IUnitOfWork unitOfWork,
        IPaymentOrchestrationService orchestrationService,
        IClock clock,
        ILogger<DunningOrchestratorJob> logger)
    {
        _unitOfWork = unitOfWork;
        _orchestrationService = orchestrationService;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var db = (DbContext)_unitOfWork;
        var now = _clock.UtcNow;

        // Active links that have a scheduled retry time and haven't exceeded attempts/deadline
        var pendingRecoveries = await db.Set<PaymentLink>()
            .Include(l => l.Loan)
            .Where(l => l.Status == PaymentLinkStatus.Active)
            .Where(l => l.NextRecoveryAttemptAt != null && l.NextRecoveryAttemptAt <= now)
            .Where(l => l.RecoveryAttemptCount < l.MaxRecoveryAttempts)
            .Where(l => l.RecoveryDeadline == null || l.RecoveryDeadline > now)
            .Where(l => !l.RecoveryInProgress)
            // Lightweight efficiency guard: ignore disputed or closed loans at the scan level
            .Where(l => l.Loan == null || (l.Loan.Status != LoanStatus.Disputed && l.Loan.Status != LoanStatus.Closed))
            .ToListAsync(ct);

        if (pendingRecoveries.Count == 0)
            return;

        _logger.LogInformation("Dunning Engine: Processing {Count} pending recoveries.", pendingRecoveries.Count);

        foreach (var link in pendingRecoveries)
        {
            // Atomic Guard: Try to acquire the recovery lock
            if (!link.TryAcquireRecoveryLock())
            {
                _logger.LogWarning("Dunning Engine: Skipped Link {LinkId}, already in progress.", link.Id);
                continue;
            }

            try
            {
                // We pass null for failure code because this is a retry of a PREVIOUS failure
                await _orchestrationService.HandlePaymentFailureAsync(
                    link.CustomerId,
                    link.RecoveryFailureReason,
                    link.AmountSnapshot.Amount,
                    link.AmountSnapshot.Currency,
                    $"Automated Dunning Attempt {link.RecoveryAttemptCount + 1}",
                    link.StripePaymentIntentId,
                    link.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dunning Engine: Failed to process recovery for Link {LinkId}", link.Id);
                link.ReleaseRecoveryLock(); // Ensure it's not stuck if the service itself crashes
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}

using Cobryx.Application.Payments.Services;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that triggers self-healing for stuck payment links.
/// </summary>
public class FinancialReconciliationJob
{
    private readonly PaymentLinkReconciliationService _reconciliationService;
    private readonly ILogger<FinancialReconciliationJob> _logger;

    public FinancialReconciliationJob(
        PaymentLinkReconciliationService reconciliationService,
        ILogger<FinancialReconciliationJob> logger)
    {
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Financial Reconciliation Job (Stuck Link Recovery)");

        // Recover links stuck for more than 30 minutes
        await _reconciliationService.RecoverStuckProcessingLinksAsync(TimeSpan.FromMinutes(30), ct);

        _logger.LogInformation("Financial Reconciliation Job completed.");
    }
}

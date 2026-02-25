using Cobryx.Application.Admin.Queries.GetFinancialMetrics;
using Cobryx.Application.Admin.Queries.GetLedgerHealth;
using Cobryx.Application.Common.Interfaces;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

public class CheckSystemHealthJob
{
    private readonly ISender _sender;
    private readonly IAlertingService _alertingService;
    private readonly ILogger<CheckSystemHealthJob> _logger;

    public CheckSystemHealthJob(
        ISender sender,
        IAlertingService alertingService,
        ILogger<CheckSystemHealthJob> logger)
    {
        _sender = sender;
        _alertingService = alertingService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting scheduled system health check...");

        // 1. Audit Ledger Integrity (Critical Path)
        await AuditLedgerHealthAsync(ct);

        // 2. Audit Financial Risk (Core Growth Path)
        await AuditFinancialMetricsAsync(ct);

        _logger.LogInformation("System health check completed.");
    }

    private async Task AuditLedgerHealthAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetLedgerHealthQuery(), ct);
        if (result.IsFailure)
        {
            await _alertingService.SendAlertAsync(
                "HealthMonitor",
                "FAILED to execute Ledger Health Query.",
                AlertLevel.Critical,
                result.Error,
                ct);
            return;
        }

        var health = result.Value;
        if (health != null && health.Status != "HEALTHY")
        {
            var level = health.Status == "CRITICAL" ? AlertLevel.Critical : AlertLevel.Degraded;
            await _alertingService.SendAlertAsync(
                "LedgerHealth",
                $"Ledger Health is {health.Status}.",
                level,
                health,
                ct);
        }
    }

    private async Task AuditFinancialMetricsAsync(CancellationToken ct)
    {
        // For the automated job, we audit aggregated metrics first.
        var result = await _sender.Send(new GetFinancialMetricsQuery(null), ct);
        if (result.IsFailure)
        {
            _logger.LogError("Failed to fetch aggregate financial metrics for health check.");
            return;
        }

        var metrics = result.Value;
        if (metrics == null) return;

        // Fintech Thresholds - Elite Standard
        if (metrics.PAR30Percentage > 15.0m) // 15% PAR30
        {
            await _alertingService.SendAlertAsync(
                "RiskMonitor",
                $"CRITICAL: Portfolio at Risk (PAR30) is {metrics.PAR30Percentage:N2}%.",
                AlertLevel.Critical,
                metrics,
                ct);
        }
        else if (metrics.PAR30Percentage > 10.0m) // 10% PAR30 warning
        {
            await _alertingService.SendAlertAsync(
                "RiskMonitor",
                $"High Risk: PAR30 is growing ({metrics.PAR30Percentage:N2}%).",
                AlertLevel.Degraded,
                metrics,
                ct);
        }

        if (metrics.PAR90Percentage > 8.0m) // 8% PAR90
        {
            await _alertingService.SendAlertAsync(
                "RiskMonitor",
                $"CRITICAL: Portfolio at Risk (90+ days) is dangerously high: {metrics.PAR90Percentage:N2}%.",
                AlertLevel.Critical,
                metrics,
                ct);
        }
    }
}

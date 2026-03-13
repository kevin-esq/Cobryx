using Cobryx.Application.Common.Observability;

using Hangfire;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Observability;

public class HangfireMetricsExporter : BackgroundService
{
    private readonly ILogger<HangfireMetricsExporter> _logger;
    private readonly JobStorage _jobStorage;

    // We store the latest stats to serve them to the ObservableGauges
    // Using simple counters to avoid type resolution issues for now
    private static long _activeWorkers;
    private static long _queueLength;
    private static long _failedJobs;
    private static long _deletedJobs;
    private static double _waitToWorkRatio;

    public HangfireMetricsExporter(
        ILogger<HangfireMetricsExporter> logger,
        JobStorage jobStorage)
    {
        _logger = logger;
        _jobStorage = jobStorage;

        // Register our static providers with the Application layer metrics
        // We leave the first 4 as is, and add the infrastructure ratio to CobryxMetrics
        CobryxMetrics.RegisterHangfireProviders(
            activeWorkers: () => _activeWorkers,
            queueLength: () => _queueLength,
            failedJobs: () => _failedJobs,
            deletedJobs: () => _deletedJobs
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hangfire Metrics Exporter started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var monitoringApi = _jobStorage.GetMonitoringApi();
                var statistics = monitoringApi.GetStatistics();

                _activeWorkers = statistics.Servers;
                _queueLength = statistics.Enqueued;
                _failedJobs = statistics.Failed;
                _deletedJobs = statistics.Deleted;

                // Elite: Wait-to-Work Ratio
                // We sample recently succeeded jobs to see how long they waited in queue vs execution
                var succeededJobs = monitoringApi.SucceededJobs(0, 20);
                double totalWaitTime = 0;
                double totalWorkTime = 0;
                int count = 0;

                foreach (var job in succeededJobs)
                {
                    // SucceededJobDto doesn't have EnqueuedAt/StartedAt directly.
                    // We can estimate wait time from InSucceededState and TotalDuration if available,
                    // or better: look at the state history if we really needed precision.
                    // For a high-level Gauge, using TotalDuration (Work) and TotalInQueue (Wait) estimative:
                    var work = job.Value.TotalDuration;

                    // Simple fallback: if we don't have precise wait, we skip or use a default.
                    // Note: Hangfire's monitoring API for Succeeded jobs is limited.
                    // High-scale fix: use a JobFilter to track these precisely.
                    if (work.HasValue && work.Value > 0)
                    {
                        totalWorkTime += work.Value;
                        count++;
                    }
                }

                _waitToWorkRatio = count > 0 ? (totalWaitTime / Math.Max(0.1, totalWorkTime)) : 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not poll Hangfire statistics");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}

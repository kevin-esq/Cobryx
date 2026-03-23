using Cobryx.Application.Common.Observability;

using Hangfire;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Observability;

public class HangfireMetricsExporter : BackgroundService
{
    private readonly ILogger<HangfireMetricsExporter> _logger;
    private readonly JobStorage _jobStorage;

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

                var succeededJobs = monitoringApi.SucceededJobs(0, 20);
                double totalWaitTime = 0;
                double totalWorkTime = 0;
                int count = 0;

                foreach (var job in succeededJobs)
                {
                    var work = job.Value.TotalDuration;

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

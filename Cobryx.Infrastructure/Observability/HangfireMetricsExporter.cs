using Cobryx.Application.Common.Observability;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
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

    public HangfireMetricsExporter(
        ILogger<HangfireMetricsExporter> logger,
        JobStorage jobStorage)
    {
        _logger = logger;
        _jobStorage = jobStorage;

        // Register our static providers with the Application layer metrics
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not poll Hangfire statistics");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}

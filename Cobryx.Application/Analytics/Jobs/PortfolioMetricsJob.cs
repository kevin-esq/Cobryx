using Cobryx.Application.Analytics.Services;
using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Analytics.Jobs
{
    public partial class PortfolioMetricsJob(
        IPortfolioAnalyticsService analyticsService,
        IClock clock,
        ILogger<PortfolioMetricsJob> logger)
    {
        public async Task RunAsync(CancellationToken ct = default)
        {
            LogStartingJob(logger);

            DateTime date = clock.UtcNow.Date;

            try
            {
                await analyticsService.CalculateAllNightlyMetricsAsync(date, ct);
                LogJobFinished(logger);
            }
            catch (Exception ex)
            {
                LogJobFailed(logger, ex);
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Starting daily multi-tenant PortfolioMetricsJob")]
        static partial void LogStartingJob(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information,
            Message = "Finished daily multi-tenant PortfolioMetricsJob successfully")]
        static partial void LogJobFinished(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to calculate portfolio metrics batch.")]
        static partial void LogJobFailed(ILogger logger, Exception ex);
    }
}

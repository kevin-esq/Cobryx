using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Analytics;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Analytics.Jobs
{
    public partial class PortfolioCacheRefreshJob(
        ICobryxDbContext db,
        ICacheService cache,
        IClock clock,
        ILogger<PortfolioCacheRefreshJob> logger)
    {
        public async Task RunAsync(CancellationToken ct = default)
        {
            LogStartingJob(logger);

            DateTime date = clock.UtcNow.Date;

            List<PortfolioMetricsDaily> metricsList = await db.PortfolioMetricsDaily
                .Where(x => x.Date == date)
                .ToListAsync(ct);

            foreach (PortfolioMetricsDaily m in metricsList)
            {
                var summaryCache = new PortfolioSummaryCache
                {
                    TenantId = m.TenantId,
                    TotalLoans = m.TotalLoans,
                    TotalOutstanding = m.TotalOutstanding,
                    AverageLoanSize = m.TotalLoans > 0 ? m.TotalPrincipal / m.TotalLoans : 0,
                    NplRatio = m.NPLRatio,
                    DelinquencyRate = m.DelinquencyRate,
                    RevenueMTD = m.RevenueMTD,
                    RevenueYTD = m.RevenueYTD,
                    CollectionEfficiency = m.CollectionEfficiency,
                    CashOutflowMTD = 0,
                    LastUpdated = clock.UtcNow
                };

                var summaryKey = $"portfolio:summary:{m.TenantId}";
                await cache.SetAsync(summaryKey, summaryCache, TimeSpan.FromHours(24), ct);

                var agingCache = new PortfolioAgingCache
                {
                    Current = Math.Max(0,
                        m.TotalOutstanding - (m.Bucket0To30 + m.Bucket31To60 + m.Bucket61To90 + m.Bucket90Plus)),
                    Bucket0To30 = m.Bucket0To30,
                    Bucket31To60 = m.Bucket31To60,
                    Bucket61To90 = m.Bucket61To90,
                    Bucket90Plus = m.Bucket90Plus
                };

                var agingKey = $"portfolio:aging:{m.TenantId}";
                await cache.SetAsync(agingKey, agingCache, TimeSpan.FromHours(24), ct);
            }

            LogJobFinished(logger, metricsList.Count);
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Starting PortfolioCacheRefreshJob to cache Redis portfolio analytics.")]
        static partial void LogStartingJob(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information,
            Message = "PortfolioCacheRefreshJob successfully cached {Count} tenants.")]
        static partial void LogJobFinished(ILogger logger, int count);
    }
}

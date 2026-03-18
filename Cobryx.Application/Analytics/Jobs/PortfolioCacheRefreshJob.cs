using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Analytics.Jobs;

public class PortfolioCacheRefreshJob(
    ICobryxDbContext db,
    ICacheService cache,
    ILogger<PortfolioCacheRefreshJob> logger)
{
    private readonly ICobryxDbContext _db = db;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<PortfolioCacheRefreshJob> _logger = logger;

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting PortfolioCacheRefreshJob to cache Redis portfolio analytics.");
        
        var date = DateTime.UtcNow.Date;

        var metricsList = await _db.PortfolioMetricsDaily
            .Where(x => x.Date == date)
            .ToListAsync(ct);

        foreach (var m in metricsList)
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
                CashInflowMTD = 0, // Placeholder, calculated from actual Cashflow event if implemented
                CashOutflowMTD = 0, // Placeholder
                LastUpdated = DateTime.UtcNow
            };

            var summaryKey = $"portfolio:summary:{m.TenantId}";
            await _cache.SetAsync(summaryKey, summaryCache, TimeSpan.FromHours(24), ct);

            var agingCache = new PortfolioAgingCache
            {
                Current = Math.Max(0, m.TotalOutstanding - (m.Bucket0To30 + m.Bucket31To60 + m.Bucket61To90 + m.Bucket90Plus)),
                Bucket0To30 = m.Bucket0To30,
                Bucket31To60 = m.Bucket31To60,
                Bucket61To90 = m.Bucket61To90,
                Bucket90Plus = m.Bucket90Plus
            };

            var agingKey = $"portfolio:aging:{m.TenantId}";
            await _cache.SetAsync(agingKey, agingCache, TimeSpan.FromHours(24), ct);
        }

        _logger.LogInformation("PortfolioCacheRefreshJob successfully cached {Count} tenants.", metricsList.Count);
    }
}

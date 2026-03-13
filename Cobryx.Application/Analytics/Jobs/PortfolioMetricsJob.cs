using Cobryx.Application.Analytics.Services;
using Cobryx.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Analytics.Jobs;

public class PortfolioMetricsJob
{
    private readonly IPortfolioAnalyticsService _analyticsService;
    private readonly ICobryxDbContext _db;
    private readonly ILogger<PortfolioMetricsJob> _logger;

    public PortfolioMetricsJob(
        IPortfolioAnalyticsService analyticsService,
        ICobryxDbContext db,
        ILogger<PortfolioMetricsJob> logger)
    {
        _analyticsService = analyticsService;
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting daily PortfolioMetricsJob");

        var tenants = await _db.Tenants.Select(t => t.Id).ToListAsync();
        var date = DateTime.UtcNow.Date;

        foreach (var tenantId in tenants)
        {
            try
            {
                _logger.LogInformation("Calculating portfolio metrics for Tenant {TenantId} on {Date}", tenantId, date);
                await _analyticsService.CalculateNightlyMetricsAsync(tenantId, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate portfolio metrics for Tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Finished daily PortfolioMetricsJob for {Count} tenants", tenants.Count);
    }
}

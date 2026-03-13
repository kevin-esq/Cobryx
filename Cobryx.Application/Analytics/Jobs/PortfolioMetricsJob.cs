using Cobryx.Application.Analytics.Services;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Analytics.Jobs;

public class PortfolioMetricsJob(
    IPortfolioAnalyticsService analyticsService,
    ILogger<PortfolioMetricsJob> logger)
{
    private readonly IPortfolioAnalyticsService _analyticsService = analyticsService;
    private readonly ILogger<PortfolioMetricsJob> _logger = logger;

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting daily multi-tenant PortfolioMetricsJob");

        var date = DateTime.UtcNow.Date;

        try
        {
            await _analyticsService.CalculateAllNightlyMetricsAsync(date);
            _logger.LogInformation("Finished daily multi-tenant PortfolioMetricsJob successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate portfolio metrics batch.");
        }
    }
}

using Asp.Versioning;
using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.v1.Analytics;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portfolio")]
[Authorize]
public class PortfolioController(ITenantProvider tenantProvider, ICacheService cache) : ControllerBase
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly ICacheService _cache = cache;

    /// <summary>
    /// Returns the 12-metric fintech profile for the portfolio. (Served from Redis &lt;10ms)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PortfolioSummaryCache), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSummary()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var key = $"portfolio:summary:{tenantId}";
        var summary = await _cache.GetAsync<PortfolioSummaryCache>(key);

        if (summary == null)
            return NotFound(new { Message = "Portfolio metrics not generated yet for today." });

        return Ok(summary);
    }

    /// <summary>
    /// Returns the standard aging buckets (Current, 1-30, 31-60, 61-90, 90+ DPD). (Served from Redis &lt;10ms)
    /// </summary>
    [HttpGet("aging")]
    [ProducesResponseType(typeof(PortfolioAgingCache), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAging()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var key = $"portfolio:aging:{tenantId}";
        var aging = await _cache.GetAsync<PortfolioAgingCache>(key);

        if (aging == null)
            return NotFound(new { Message = "Portfolio aging metrics not generated yet for today." });

        return Ok(aging);
    }

    /// <summary>
    /// Ultra-fast endpoint for the home dashboard returning the 4 most critical business operations metrics.
    /// </summary>
    [HttpGet("../dashboard/kpis")] // Maps to /api/v1/dashboard/kpis
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCoreKpis()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var key = $"portfolio:summary:{tenantId}";
        var summary = await _cache.GetAsync<PortfolioSummaryCache>(key);

        if (summary == null)
            return NotFound(new { Message = "KPIs not generated yet for today." });

        var kpis = new
        {
            totalOutstanding = summary.TotalOutstanding,
            nplRatio = summary.NplRatio,
            revenueMTD = summary.RevenueMTD,
            collectionEfficiency = summary.CollectionEfficiency
        };

        return Ok(kpis);
    }
}

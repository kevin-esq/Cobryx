using Cobryx.Application.Common.Interfaces;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
public class RiskDashboardController(ICacheService cache, ISender sender) : CobryxBaseController(sender)
{
    [HttpGet("/api/v1/risk/metrics")]
    public async Task<IActionResult> GetCoreMetrics()
    {
        var var1h = await cache.GetAsync<decimal?>("risk:var:1h");
        var cvar1h = await cache.GetAsync<decimal?>("risk:cvar:1h", CancellationToken.None);
        var avgM = await cache.GetAsync<decimal?>("risk:exposure_multiplier_avg:1h", CancellationToken.None);

        return Ok(new
        {
            timeWindow = "1h",
            VaR95 = var1h ?? 1.0m,
            ExpectedShortfall = cvar1h ?? 1.0m,
            AverageExposureMultiplier = avgM ?? 1.0m
        });
    }
}

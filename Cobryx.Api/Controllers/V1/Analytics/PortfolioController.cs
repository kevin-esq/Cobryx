using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Analytics.Common;
using Cobryx.Application.Analytics.Queries.GetPortfolioAging;
using Cobryx.Application.Analytics.Queries.GetPortfolioSummary;

using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Analytics;

/// <summary>
/// Provides real-time portfolio analytics served from Redis (&lt;10ms).
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portfolio")]
[Tags("Operations")]
public class PortfolioController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Returns the 12-metric fintech profile for the portfolio.
    /// </summary>
    /// <param name="ct">Cancellation token injected by ASP.NET.</param>
    /// <remarks>
    /// Served from Redis with &lt;10ms latency.
    ///
    /// Possible Outcomes:
    /// - ANALYTICS.PORTFOLIO.SUMMARY_RETRIEVED: Metrics retrieved successfully.
    /// </remarks>
    /// <response code="200">Portfolio summary metrics.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Metrics not yet generated for today.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PortfolioSummaryCache>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        Result<PortfolioSummaryCache> result = await Sender.Send(new GetPortfolioSummaryQuery(), ct);
        return HandleResult(result, AnalyticsOutcomes.Portfolio.SummaryRetrieved);
    }

    /// <summary>
    /// Returns the standard aging buckets (Current, 1-30, 31-60, 61-90, 90+ DPD).
    /// </summary>
    /// <param name="ct">Cancellation token injected by ASP.NET.</param>
    /// <remarks>
    /// Served from Redis with &lt;10ms latency.
    ///
    /// Possible Outcomes:
    /// - ANALYTICS.PORTFOLIO.AGING_RETRIEVED: Aging buckets retrieved successfully.
    /// </remarks>
    /// <response code="200">Portfolio aging buckets.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Aging metrics not yet generated for today.</response>
    [HttpGet("aging")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PortfolioAgingCache>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAging(CancellationToken ct)
    {
        Result<PortfolioAgingCache> result = await Sender.Send(new GetPortfolioAgingQuery(), ct);
        return HandleResult(result, AnalyticsOutcomes.Portfolio.AgingRetrieved);
    }

    /// <summary>
    /// Returns the 4 most critical business KPIs.
    /// </summary>
    /// <param name="ct">Cancellation token injected by ASP.NET.</param>
    /// <remarks>
    /// Derived from the portfolio summary cache. Served from Redis with &lt;10ms latency.
    ///
    /// Possible Outcomes:
    /// - ANALYTICS.PORTFOLIO.KPIS_RETRIEVED: Core KPIs retrieved successfully.
    /// </remarks>
    /// <response code="200">Core business KPIs.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">KPIs not yet generated for today.</response>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CoreKpisDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCoreKpis(CancellationToken ct)
    {
        Result<PortfolioSummaryCache> result = await Sender.Send(new GetPortfolioSummaryQuery(), ct);

        if (!result.IsSuccess || result.Value is null)
            return HandleResult(result, AnalyticsOutcomes.Portfolio.KpisRetrieved);

        CoreKpisDto kpis = MapToCoreKpis(result.Value);

        return Success(kpis, AnalyticsOutcomes.Portfolio.KpisRetrieved);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static CoreKpisDto MapToCoreKpis(PortfolioSummaryCache summary) =>
        new(summary.TotalOutstanding,
            summary.NplRatio,
            summary.RevenueMTD,
            summary.CollectionEfficiency);
}

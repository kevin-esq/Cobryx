using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Analytics.Models;
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
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portfolio")]
[Authorize]
[Tags("Operations")]
public class PortfolioController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Returns the 12-metric fintech profile for the portfolio.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Served from Redis with &lt;10ms latency.
    ///
    /// Possible Outcomes:
    /// - ANALYTICS.PORTFOLIO.SUMMARY_RETRIEVED: Metrics retrieved successfully.
    /// </remarks>
    /// <response code="200">Portfolio summary metrics.</response>
    /// <response code="404">Metrics not generated yet for today.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PortfolioSummaryCache>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        Result<PortfolioSummaryCache> result = await Sender.Send(new GetPortfolioSummaryQuery(), ct);
        return HandleResult(result, AnalyticsOutcomes.Portfolio.SummaryRetrieved);
    }

    /// <summary>
    /// Returns the standard aging buckets (Current, 1-30, 31-60, 61-90, 90+ DPD).
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Served from Redis with &lt;10ms latency.
    ///
    /// Possible Outcomes:
    /// - ANALYTICS.PORTFOLIO.AGING_RETRIEVED: Aging buckets retrieved successfully.
    /// </remarks>
    /// <response code="200">Portfolio aging buckets.</response>
    /// <response code="404">Aging metrics not generated yet for today.</response>
    [HttpGet("aging")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PortfolioAgingCache>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetAging(CancellationToken ct)
    {
        Result<PortfolioAgingCache> result = await Sender.Send(new GetPortfolioAgingQuery(), ct);
        return HandleResult(result, AnalyticsOutcomes.Portfolio.AgingRetrieved);
    }

    /// <summary>
    /// Ultra-fast endpoint returning the 4 most critical business KPIs.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ANALYTICS.PORTFOLIO.KPIS_RETRIEVED: Core KPIs retrieved successfully.
    /// </remarks>
    /// <response code="200">Core business KPIs.</response>
    /// <response code="404">KPIs not generated yet for today.</response>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CoreKpisDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetCoreKpis(CancellationToken ct)
    {
        Result<PortfolioSummaryCache> result = await Sender.Send(new GetPortfolioSummaryQuery(), ct);

        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result, AnalyticsOutcomes.Portfolio.KpisRetrieved);

        var kpis = new CoreKpisDto(
            result.Value.TotalOutstanding,
            result.Value.NplRatio,
            result.Value.RevenueMTD,
            result.Value.CollectionEfficiency);

        return Success(kpis, AnalyticsOutcomes.Portfolio.KpisRetrieved);
    }
}

public record CoreKpisDto(
    decimal TotalOutstanding,
    decimal NplRatio,
    decimal RevenueMTD,
    decimal CollectionEfficiency);

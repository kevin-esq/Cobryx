using Asp.Versioning;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Dashboard;
using Cobryx.Application.Dashboard.Common;
using Cobryx.Application.Dashboard.Queries.GetOnboardingStatus;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Provides aggregated operational metrics and onboarding status for the tenant dashboard.
/// </summary>
[Authorize(Policy = "CanManageTenant")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Tags("Operations")]
public class DashboardController(ISender sender, ICacheService cacheService, ITenantProvider tenantProvider)
    : CobryxBaseController(sender)
{
    /// <summary>
    /// Retrieves a high-level operational summary for the tenant dashboard.
    /// Results are cached for 5 minutes to ensure high performance.
    /// </summary>
    /// <remarks>
    /// This endpoint aggregates data from Loans, Customers, and Payments modules.
    /// Use this as the primary data source for landing page charts and metrics.
    ///
    /// Possible Outcomes:
    /// - DASHBOARD.SUMMARY_RETRIEVED: Statistics successfully aggregated.
    /// </remarks>
    /// <response code="200">The current dashboard summary metrics.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiSuccessResponse<DashboardSummaryDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetSummary()
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        var cacheKey = $"dash:{tenantId}:summary";

        DashboardSummaryDto? cachedSummary = await cacheService.GetAsync<DashboardSummaryDto>(cacheKey);
        if (cachedSummary != null)
        {
            return Success(cachedSummary);
        }

        Result<DashboardSummaryDto> result = await Sender.Send(new GetDashboardSummaryQuery());

        if (result.IsSuccess)
        {
            await cacheService.SetAsync(cacheKey, result.Value!, TimeSpan.FromMinutes(5));
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves the onboarding status for the current tenant.
    /// Tracks setup progress and value-realization signals.
    /// </summary>
    [HttpGet("onboarding-status")]
    [ProducesResponseType(typeof(ApiSuccessResponse<OnboardingStatusDto>), 200)]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        Result<OnboardingStatusDto> result = await Sender.Send(new GetOnboardingStatusQuery());
        return HandleResult(result);
    }
}

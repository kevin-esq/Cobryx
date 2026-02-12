using Cobryx.Application.Dashboard;
using Cobryx.Infrastructure.Caching;
using Cobryx.Application.Common.Interfaces;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize(Policy = "CanManageTenant")]
[Route("api/dashboard")]
public class DashboardController : CobryxBaseController
{
    private readonly ICacheService _cacheService;
    private readonly ITenantProvider _tenantProvider;

    public DashboardController(ISender sender, ICacheService cacheService, ITenantProvider tenantProvider) 
        : base(sender)
    {
        _cacheService = cacheService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var cacheKey = $"dash:{tenantId}:summary";

        var cachedSummary = await _cacheService.GetAsync<DashboardSummaryDto>(cacheKey);
        if (cachedSummary != null)
        {
            return Success(cachedSummary);
        }

        var result = await Sender.Send(new GetDashboardSummaryQuery());

        if (result.IsSuccess)
        {
            await _cacheService.SetAsync(cacheKey, result.Value!, TimeSpan.FromMinutes(5));
        }

        return HandleResult(result);
    }
}

using Asp.Versioning;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Tenants.Commands.UpdateBusinessSettings;
using Cobryx.Application.Tenants.Commands.UpdateTenantSettings;
using Cobryx.Application.Tenants.Commands.SeedDemoData;
using Cobryx.Application.Tenants.Queries.GetBusinessSettings;
using Cobryx.Application.Tenants.Queries.GetTenantSettings;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages tenant profile, branding, and business configuration defaults.
/// All settings are scoped to the authenticated tenant context.
/// </summary>
[Authorize(Policy = "CanManageTenant")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
[Tags("Platform")]
public class TenantSettingsController : CobryxBaseController
{
    public TenantSettingsController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Retrieves the current tenant profile including branding, contact info, and onboarding status.
    /// </summary>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - TENANT.SEARCH.COMPLETED: Tenant settings retrieved successfully.
    /// </remarks>
    /// <response code="200">The tenant profile and branding configuration.</response>
    [HttpGet("tenant")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<TenantSettingsDto>), 200)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> GetTenantSettings()
    {
        var result = await Sender.Send(new GetTenantSettingsQuery());
        return HandleResult(result, TenantOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Updates tenant branding and contact information.
    /// </summary>
    /// <param name="request">Fields to update. Pass null for any field to clear it.</param>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - TENANT.BRANDING.UPDATED: Tenant profile updated successfully.
    /// </remarks>
    /// <response code="200">Update confirmed.</response>
    [HttpPatch("tenant")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> UpdateTenantSettings([FromBody] UpdateTenantSettingsCommand request)
    {
        var result = await Sender.Send(request);
        return HandleResult(result, TenantOutcomes.BrandingUpdated);
    }

    /// <summary>
    /// Retrieves the tenant's business configuration defaults (interest, penalties, payment rules).
    /// </summary>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - TENANT.SEARCH.COMPLETED: Business settings retrieved successfully.
    /// </remarks>
    /// <response code="200">The business configuration defaults.</response>
    [HttpGet("business")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<BusinessSettingsDto>), 200)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> GetBusinessSettings()
    {
        var result = await Sender.Send(new GetBusinessSettingsQuery());
        return HandleResult(result, TenantOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Replaces the tenant's business configuration defaults.
    /// </summary>
    /// <param name="request">All business settings fields are required — this is a full replacement.</param>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - TENANT.SETTINGS.UPDATED: Business settings updated successfully.
    /// </remarks>
    /// <response code="200">Update confirmed.</response>
    [HttpPatch("business")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> UpdateBusinessSettings([FromBody] UpdateBusinessSettingsCommand request)
    {
        var result = await Sender.Send(request);
        return HandleResult(result, TenantOutcomes.SettingsUpdated);
    }

    /// <summary>
    /// Seeds the current tenant with demonstration data (Mock Loans/Payments).
    /// Used to reduce Time-To-Wow for new accounts.
    /// </summary>
    [HttpPost("demo-seed")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse), 200)]
    public async Task<IActionResult> SeedDemoData()
    {
        var result = await Sender.Send(new SeedDemoDataCommand());
        return HandleResult(result);
    }
}

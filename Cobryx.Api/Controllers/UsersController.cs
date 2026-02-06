using Cobryx.Application.Users.Queries.GetUsers;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;

namespace Cobryx.Api.Controllers;

[Authorize(Policy = "CanManageTenant")]
[ApiController]
[Route("api/users")]
public class UsersController : CobryxBaseController
{
    public UsersController(ISender sender) : base(sender)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new GetUsersQuery(page, pageSize));
        return HandleResult(result, UserOutcomes.SearchCompleted);
    }
}

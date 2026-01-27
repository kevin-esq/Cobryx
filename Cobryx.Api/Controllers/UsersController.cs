using Cobryx.Application.Users.Queries.GetUsers;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize(Policy = "CanManageTenant")]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new GetUsersQuery(page, pageSize));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

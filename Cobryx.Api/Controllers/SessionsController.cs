using Cobryx.Application.Auth.Commands.Sessions;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sessions")]
public class SessionsController : CobryxBaseController
{
    public SessionsController(ISender sender) : base(sender)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        var result = await Sender.Send(new GetSessionsQuery());
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        var result = await Sender.Send(new RevokeSessionCommand(id));
        return HandleResult(result, "Session revoked successfully.");
    }
}

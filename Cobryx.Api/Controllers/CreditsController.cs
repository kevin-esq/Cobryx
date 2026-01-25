using Cobryx.Application.Credits.Commands.Create;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize(Policy = "CanCreateCredits")]
[ApiController]
[Route("api/credits")]
public class CreditsController : ControllerBase
{
    private readonly ISender _sender;

    public CreditsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCreditCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

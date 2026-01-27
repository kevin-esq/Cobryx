using Cobryx.Application.Credits.Commands.Create;
using Cobryx.Application.Credits.Queries.GetCredits;
using Cobryx.Application.Credits.Queries.GetCreditById;
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

    [HttpGet]
    [Authorize(Policy = "CanViewCredits")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new GetCreditsQuery(customerId, page, pageSize));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewCredits")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new GetCreditByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}

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
public class CreditsController : CobryxBaseController
{
    public CreditsController(ISender sender) : base(sender)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCreditCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Credit created successfully");
    }

    [HttpGet]
    [Authorize(Policy = "CanViewCredits")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new GetCreditsQuery(customerId, page, pageSize));
        return HandleResult(result, "Credits retrieved successfully");
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewCredits")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Sender.Send(new GetCreditByIdQuery(id));
        return HandleResult(result, "Credit retrieved successfully");
    }
}

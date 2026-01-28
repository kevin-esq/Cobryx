using Cobryx.Application.Support.Commands.Create;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/support")]
public class SupportController : CobryxBaseController
{
    public SupportController(ISender sender) : base(sender)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupportTicketCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Support ticket created successfully");
    }
}

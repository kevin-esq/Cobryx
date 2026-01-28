using Cobryx.Application.Financial.Commands.CreateInvoice;
using Cobryx.Application.Financial.Queries.GetInvoices;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly ISender _sender;

    public InvoicesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices()
    {
        var result = await _sender.Send(new GetInvoicesQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

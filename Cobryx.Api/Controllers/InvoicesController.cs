using Cobryx.Application.Invoicing.Commands.CreateInvoice;
using Cobryx.Application.Invoicing.Queries.GetInvoices;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/invoices")]
public class InvoicesController : CobryxBaseController
{
    public InvoicesController(ISender sender) : base(sender)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices()
    {
        var result = await Sender.Send(new GetInvoicesQuery());
        return HandleResult(result, InvoicingOutcomes.Invoices.SearchCompleted);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, InvoicingOutcomes.Invoices.Created);
    }
}

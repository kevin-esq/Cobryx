using Cobryx.Application.Invoicing.Commands.CreateTaxConfiguration;
using Cobryx.Application.Invoicing.Commands.DeleteTaxConfiguration;
using Cobryx.Application.Invoicing.Queries.GetTaxConfigurations;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/taxes")]
public class TaxesController : CobryxBaseController
{
    public TaxesController(ISender sender) : base(sender)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetTaxes()
    {
        var result = await Sender.Send(new GetTaxConfigurationsQuery());
        return HandleResult(result, InvoicingOutcomes.Taxes.SearchCompleted);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTax(CreateTaxConfigurationCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, InvoicingOutcomes.Taxes.Created);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTax(Guid id)
    {
        var result = await Sender.Send(new DeleteTaxConfigurationCommand(id));
        return HandleResult(result, InvoicingOutcomes.Taxes.Deleted);
    }
}

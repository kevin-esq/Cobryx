using Cobryx.Application.Financial.Commands.CreateTaxConfiguration;
using Cobryx.Application.Financial.Commands.DeleteTaxConfiguration;
using Cobryx.Application.Financial.Queries.GetTaxConfigurations;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/taxes")]
public class TaxesController : ControllerBase
{
    private readonly ISender _sender;

    public TaxesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetTaxes()
    {
        var result = await _sender.Send(new GetTaxConfigurationsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTax(CreateTaxConfigurationCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTax(Guid id)
    {
        var result = await _sender.Send(new DeleteTaxConfigurationCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

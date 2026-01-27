using Cobryx.Application.Customers.Commands.Update;
using Cobryx.Application.Customers.Commands.Create;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize(Policy = "CanCreateCustomers")]
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateCustomers")]
    public async Task<IActionResult> Create(CreateCustomerCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewCustomers")]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new Cobryx.Application.Customers.Queries.GetCustomers.GetCustomersQuery(searchTerm, page, pageSize));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewCustomers")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new Cobryx.Application.Customers.Queries.GetCustomerById.GetCustomerByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanCreateCustomers")] // Or a specific CanUpdate policy if defined
    public async Task<IActionResult> Update(Guid id, UpdateCustomerCommand command)
    {
        if (id != command.Id) return BadRequest("Mismatched ID");
        var result = await _sender.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanCreateCustomers")] // Or a specific CanDelete policy
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _sender.Send(new Cobryx.Application.Customers.Commands.Delete.DeleteCustomerCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}

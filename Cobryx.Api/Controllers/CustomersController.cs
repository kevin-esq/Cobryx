using Cobryx.Application.Customers.Commands.Update;
using Cobryx.Application.Customers.Commands.Create;
using Cobryx.Application.Common.Models;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize(Policy = "CanCreateCustomers")]
[Authorize(Policy = "CanCreateCustomers")]
[ApiController]
[Route("api/customers")]
public class CustomersController : CobryxBaseController
{
    public CustomersController(ISender sender) : base(sender)
    {
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateCustomers")]
    public async Task<IActionResult> Create(CreateCustomerCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Customer created successfully");
    }

    [HttpGet]
    [Authorize(Policy = "CanViewCustomers")]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new Cobryx.Application.Customers.Queries.GetCustomers.GetCustomersQuery(searchTerm, page, pageSize));
        return HandleResult(result, "Customers retrieved successfully");
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewCustomers")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Sender.Send(new Cobryx.Application.Customers.Queries.GetCustomerById.GetCustomerByIdQuery(id));
        return HandleResult(result, "Customer retrieved successfully");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanCreateCustomers")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerCommand command)
    {
        if (id != command.Id) throw new FluentValidation.ValidationException("Mismatched ID");
        var result = await Sender.Send(command);
        return HandleResult(result, "Customer updated successfully");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanCreateCustomers")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await Sender.Send(new Cobryx.Application.Customers.Commands.Delete.DeleteCustomerCommand(id));
        return HandleResult(result, "Customer deleted successfully");
    }
}

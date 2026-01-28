using Cobryx.Application.Financial.Commands.CreatePaymentMethod;
using Cobryx.Application.Financial.Commands.DeletePaymentMethod;
using Cobryx.Application.Financial.Queries.GetPaymentMethods;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/payment-methods")]
public class PaymentMethodsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentMethodsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var result = await _sender.Send(new GetPaymentMethodsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentMethod(CreatePaymentMethodCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        var result = await _sender.Send(new DeletePaymentMethodCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

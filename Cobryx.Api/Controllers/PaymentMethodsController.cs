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
public class PaymentMethodsController : CobryxBaseController
{
    public PaymentMethodsController(ISender sender) : base(sender)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var result = await Sender.Send(new GetPaymentMethodsQuery());
        return HandleResult(result, "Payment methods retrieved successfully");
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentMethod(CreatePaymentMethodCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, "Payment method created successfully");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        var result = await Sender.Send(new DeletePaymentMethodCommand(id));
        return HandleResult(result, "Payment method deleted successfully");
    }
}

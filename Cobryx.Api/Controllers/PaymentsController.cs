using Cobryx.Application.Payments.Commands.ProcessPayment;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/payments")]
public class PaymentsController : CobryxBaseController
{
    public PaymentsController(ISender sender) : base(sender)
    {
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment(ProcessPaymentCommand command)
    {
        var result = await Sender.Send(command);
        return HandleResult(result, InvoicingOutcomes.Payments.Completed);
    }
}

using Cobryx.Application.Invoicing.Commands.CreatePaymentMethod;
using Cobryx.Application.Invoicing.Commands.DeletePaymentMethod;
using Cobryx.Application.Invoicing.Queries.GetPaymentMethods;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Financial;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;
using System.Threading.Tasks;
using System;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for managing tenant payment methods (Bank accounts, Cash, POS).
/// </summary>
[Authorize]
[ApiController]
[Route("api/financial/payment-methods")]
[Tags("Financial Core")]
public class PaymentMethodsController : CobryxBaseController
{
    public PaymentMethodsController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Lists all active payment methods for the current tenant.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT_METHOD.SEARCH.COMPLETED: Payment methods successfully retrieved.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var result = await Sender.Send(new GetPaymentMethodsQuery());
        return HandleResult(result, InvoicingOutcomes.PaymentMethods.SearchCompleted);
    }

    /// <summary>
    /// Registers a new payment method for receiving customer payments.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT_METHOD.CREATED: Payment method successfully registered.
    /// - FINANCIAL.PAYMENT_METHOD.FAILED: Validation error or duplicate code.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
    {
        // Intentional Mapping: Public Intent -> Internal Implementation
        var command = new CreatePaymentMethodCommand(
            request.Name,
            request.Code,
            request.Description);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/financial/payment-methods/{result.Value}", result, InvoicingOutcomes.PaymentMethods.Created);
    }

    /// <summary>
    /// Formally removes a payment method.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT_METHOD.DELETED: Payment method successfully deactivated.
    /// - FINANCIAL.PAYMENT_METHOD.FAILED: Payment method not found.
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        var result = await Sender.Send(new DeletePaymentMethodCommand(id));
        return HandleDeleteResult(result, InvoicingOutcomes.PaymentMethods.Deleted);
    }
}

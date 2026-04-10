using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Invoicing.Commands.CreatePaymentMethod;
using Cobryx.Application.Invoicing.Commands.DeletePaymentMethod;
using Cobryx.Application.Invoicing.Queries.GetPaymentMethods;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing tenant payment methods (Bank accounts, Cash, POS).
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment-methods")]
[Tags("Payments")]
public class PaymentMethodsController(ISender sender, IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
    /// <summary>
    /// Lists all active payment methods for the current tenant.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT_METHOD.SEARCH.COMPLETED: Payment methods successfully retrieved.
    /// </remarks>
    /// <response code="200">A collection of active payment methods.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<PaymentMethodContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var result = await Sender.Send(new GetPaymentMethodsQuery());

        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result, InvoicingOutcomes.PaymentMethods.SearchCompleted);

        var mapped = result.Value.Select(m => new PaymentMethodContract(
            m.Id,
            m.Name,
            m.Code,
            m.Description)).ToList();

        return Success(mapped, InvoicingOutcomes.PaymentMethods.SearchCompleted);
    }

    /// <summary>
    /// Registers a new payment method for receiving customer payments.
    /// </summary>
    /// <param name="request">The payment method configuration including descriptive name and unique code.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT_METHOD.CREATED: Payment method successfully registered.
    /// - FINANCIAL.PAYMENT_METHOD.FAILED: Validation error or duplicate code.
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the established payment method.</response>
    /// <response code="400">Invalid parameters or duplicate code.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
    {
        var command = new CreatePaymentMethodCommand(
            request.Name,
            request.Code,
            request.Description);

        var result = await Sender.Send(command);
        return HandleCreatedResult(linkGenerator.GetPaymentMethodUrl(result.Value), result,
            InvoicingOutcomes.PaymentMethods.Created);
    }

    /// <summary>
    /// Retrieves details for a specific payment method.
    /// </summary>
    [HttpGet("{id}", Name = "GetPaymentMethod")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PaymentMethodContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetPaymentMethod(Guid id)
    {
        var result = await Sender.Send(new GetPaymentMethodsQuery());
        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result);

        var method = result.Value.FirstOrDefault(m => m.Id == id);
        if (method == null)
            return NotFound();

        return Success(new PaymentMethodContract(method.Id, method.Name, method.Code, method.Description),
            InvoicingOutcomes.PaymentMethods.SearchCompleted);
    }

    /// <summary>
    /// Formally removes a payment method.
    /// </summary>
    /// <param name="id">Identifier of the method to remove.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT_METHOD.DELETED: Payment method successfully deactivated.
    /// - FINANCIAL.PAYMENT_METHOD.FAILED: Payment method not found.
    /// </remarks>
    /// <response code="204">Payment method successfully removed.</response>
    /// <response code="404">Payment method not found.</response>
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

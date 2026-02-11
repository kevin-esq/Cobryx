using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Financial;
using Cobryx.Api.Outcomes;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for managing commercial invoicing, billing cycles, and receivable balances.
/// Orchestrates the issuance of formal billing documents and their associated line items.
/// </summary>
[Authorize]
[ApiController]
[Route("api/financial/invoices")]
[Tags("Financial Core")]
public class InvoicesController : CobryxBaseController
{
    public InvoicesController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Retrieves a paginated list of all commercial invoices issued within the tenant context.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - INVOICING.INVOICE.SEARCH_COMPLETED: Results retrieved successfully.
    /// </remarks>
    /// <response code="200">A list of issued invoices.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetInvoices()
    {
        var result = await Sender.Send(new Application.Invoicing.Queries.GetInvoices.GetInvoicesQuery());
        return HandleResult(result, InvoicingOutcomes.Invoices.SearchCompleted);
    }

    /// <summary>
    /// Creates and issues a new commercial invoice containing one or more line items.
    /// </summary>
    /// <param name="request">Invoice details including customer context, due date, and line items with unit prices (ISO-4217).</param>
    /// <remarks>
    /// Financial Precision:
    /// - 'UnitPrice' and totals are provided in the native currency unit (ISO-4217).
    /// - 'DueDate' must follow ISO-8601.
    ///
    /// Tax configurations are applied per line item if provided.
    ///
    /// Possible Outcomes:
    /// - INVOICING.INVOICE.CREATED: Invoice issued and stored successfully.
    /// - INVOICING.INVOICE.FAILED: Validation failure (e.g., zero items or invalid quantities).
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the issued invoice.</response>
    /// <response code="400">Invalid parameters or malformed request.</response>
    /// <response code="422">Business rule violation (e.g., inactive customer or blocked billing).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        // Intentional Mapping: Public Intent -> Internal Domain Command
        var command = new Application.Invoicing.Commands.CreateInvoice.CreateInvoiceCommand(
            request.CustomerId,
            request.DueDate,
            request.Items.Select(i => new Application.Invoicing.Commands.CreateInvoice.InvoiceItemRequest(
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.TaxConfigurationId)).ToList(),
            request.Notes);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/financial/invoices/{result.Value}", result, InvoicingOutcomes.Invoices.Created);
    }
}

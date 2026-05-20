using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Invoicing.Commands.CreateInvoice;
using Cobryx.Application.Invoicing.Queries.GetInvoices;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing commercial invoicing, billing cycles, and receivable balances.
/// Orchestrates the issuance of formal billing documents and their associated line items.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoices")]
[Tags("Invoicing")]
public class InvoicesController(ISender sender, IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
    /// <summary>
    /// Retrieves a paginated list of all commercial invoices issued within the tenant context.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - INVOICING.INVOICE.SEARCH_COMPLETED: Results retrieved successfully.
    /// </remarks>
    /// <response code="200">A collection of issued invoices.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<InvoiceSummaryContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetInvoices(CancellationToken ct)
    {
        Result<IReadOnlyList<InvoiceDto>> result = await Sender.Send(new GetInvoicesQuery(), ct);

        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result, InvoicingOutcomes.Invoices.SearchCompleted);

        var mapped = result.Value.Select(i => new InvoiceSummaryContract(
            i.Id,
            i.CustomerId,
            i.CustomerName,
            i.InvoiceNumber,
            i.DueDate,
            i.TotalAmount,
            i.Currency,
            i.Status)).ToList();

        return Success(mapped, InvoicingOutcomes.Invoices.SearchCompleted);
    }

    /// <summary>
    /// Retrieves a single invoice by its identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the invoice.</param>
    [HttpGet("{id}", Name = "GetInvoice")]
    [ProducesResponseType(typeof(ApiSuccessResponse<InvoiceSummaryContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        // Fallback: Currently re-uses the list query to find the invoice
        Result<IReadOnlyList<InvoiceDto>> result = await Sender.Send(new GetInvoicesQuery());

        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result);

        InvoiceDto? invoice = result.Value.FirstOrDefault(i => i.Id == id);
        if (invoice == null)
            return NotFound();

        var mapped = new InvoiceSummaryContract(
            invoice.Id,
            invoice.CustomerId,
            invoice.CustomerName,
            invoice.InvoiceNumber,
            invoice.DueDate,
            invoice.TotalAmount,
            invoice.Currency,
            invoice.Status);

        return Success(mapped, InvoicingOutcomes.Invoices.SearchCompleted);
    }

    /// <summary>
    /// Creates and issues a new commercial invoice containing one or more line items.
    /// </summary>
    /// <param name="request">Invoice details including customer context, due date, and line items with unit prices (ISO-4217).</param>
    /// <param name="ct">Cancellation token.</param>
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
    [Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var command = new CreateInvoiceCommand(
            request.CustomerId,
            request.DueDate,
            [
                .. request.Items.Select(i => new Application.Invoicing.Commands.CreateInvoice.InvoiceItemRequest(
                    i.Description,
                    i.Quantity,
                    i.UnitPrice,
                    i.TaxConfigurationId))
            ],
            request.Notes);

        Result<Guid> result = await Sender.Send(command, ct);
        return HandleCreatedResult(linkGenerator.GetInvoiceUrl(result.Value), result,
            InvoicingOutcomes.Invoices.Created);
    }
}

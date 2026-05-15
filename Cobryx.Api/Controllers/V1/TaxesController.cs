using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Invoicing.Commands.CreateTaxConfiguration;
using Cobryx.Application.Invoicing.Commands.DeleteTaxConfiguration;
using Cobryx.Application.Invoicing.Queries.GetTaxConfigurations;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages taxation frameworks, jurisdictional rates, and inclusive/exclusive billing configurations.
/// Provides fiscal definitions used during invoice generation.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tax-configurations")]
[Tags("Tax")]
public class TaxesController(ISender sender, IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
    /// <summary>
    /// Lists all tax configurations and jurisdictional rates for the tenant.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.TAX.SEARCH.COMPLETED: Records retrieved successfully.
    /// </remarks>
    /// <response code="200">A collection of active tax configurations.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<TaxConfigurationContract>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTaxes()
    {
        Result<IReadOnlyList<TaxDto>> result = await Sender.Send(new GetTaxConfigurationsQuery());

        if (!result.IsSuccess || result.Value is null)
            return HandleResult(result, InvoicingOutcomes.Taxes.SearchCompleted);

        var contracts = result.Value
            .Select(MapToContract)
            .ToList();

        return Success(contracts, InvoicingOutcomes.Taxes.SearchCompleted);
    }

    /// <summary>
    /// Retrieves a single tax configuration by its identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the tax configuration.</param>
    /// <response code="200">Tax configuration found.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Tax configuration not found.</response>
    [HttpGet("{id}", Name = "GetTax")]
    [ProducesResponseType(typeof(ApiSuccessResponse<TaxConfigurationContract>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTax(Guid id)
    {
        Result<IReadOnlyList<TaxDto>> result = await Sender.Send(new GetTaxConfigurationsQuery());

        if (!result.IsSuccess || result.Value is null)
            return HandleResult(result);

        TaxDto? tax = result.Value.FirstOrDefault(t => t.Id == id);

        if (tax is null)
            return NotFound();

        return Success(MapToContract(tax), InvoicingOutcomes.Taxes.SearchCompleted);
    }

    /// <summary>
    /// Creates a new taxation configuration (e.g., VAT, Sales Tax).
    /// </summary>
    /// <param name="request">Tax configuration including label and statutory rate.</param>
    /// <remarks>
    /// Fiscal Precision:
    /// - <c>Rate</c> must be a decimal fraction (e.g., 0.16 for 16%).
    /// - <c>IsInclusive</c> determines whether the rate is baked into unit prices or added as a surcharge.
    ///
    /// Possible Outcomes:
    /// - FINANCIAL.TAX.CREATED: Configuration established.
    /// - FINANCIAL.TAX.VALIDATION_FAILED: Validation failed (e.g., negative rate).
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the new tax configuration.</response>
    /// <response code="400">Invalid parameters provided.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="409">Conflict: a configuration with the same attributes already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTax([FromBody] CreateTaxRequest request)
    {
        var command = new CreateTaxConfigurationCommand(
            request.Name,
            request.Rate,
            request.IsInclusive,
            request.IsDefault);

        Result<Guid> result = await Sender.Send(command);

        return HandleCreatedResult(linkGenerator.GetTaxUrl(result.Value), result, InvoicingOutcomes.Taxes.Created);
    }

    /// <summary>
    /// Removes a taxation configuration from the tenant's registry.
    /// </summary>
    /// <param name="id">Identifier of the configuration to remove.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.TAX.DELETED: Configuration successfully removed.
    /// - FINANCIAL.TAX.VALIDATION_FAILED: Configuration not found.
    /// </remarks>
    /// <response code="204">Configuration successfully removed.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTax(Guid id)
    {
        Result result = await Sender.Send(new DeleteTaxConfigurationCommand(id));
        return HandleDeleteResult(result, InvoicingOutcomes.Taxes.Deleted);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static TaxConfigurationContract MapToContract(TaxDto t) =>
        new(t.Id, t.Name, t.Rate, t.IsInclusive, t.IsDefault);
}

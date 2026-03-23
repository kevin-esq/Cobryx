using Asp.Versioning;

using Cobryx.Api.Outcomes;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing taxation frameworks, jurisdictional rates, and inclusive/exclusive billing configurations.
/// Orchestrates the fiscal definitions used during invoice generation.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/financial/taxes")]
[Tags("Financial Core")]
public class TaxesController : CobryxBaseController
{
    public TaxesController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Lists all tax configurations and jurisdictional rates established for the tenant.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.TAX.SEARCH.COMPLETED: Records retrieved successfully.
    /// </remarks>
    /// <response code="200">A collection of active tax configurations.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<TaxConfigurationContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetTaxes()
    {
        var result = await Sender.Send(new Application.Invoicing.Queries.GetTaxConfigurations.GetTaxConfigurationsQuery());

        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result, InvoicingOutcomes.Taxes.SearchCompleted);

        var mapped = result.Value.Select(t => new TaxConfigurationContract(
            t.Id,
            t.Name,
            t.Rate,
            t.IsInclusive,
            t.IsDefault
        )).ToList();

        return Success(mapped, InvoicingOutcomes.Taxes.SearchCompleted);
    }

    /// <summary>
    /// Establishes a new taxation configuration (e.g., VAT, Sales Tax) with specific rules.
    /// </summary>
    /// <param name="request">Tax configuration including descriptive label and statutory rate.</param>
    /// <remarks>
    /// Fiscal Precision:
    /// - 'Rate' should be provided as a decimal fraction (e.g., 0.16 for 16%).
    /// - 'IsInclusive' determines if the rate is already baked into unit prices or added as a surcharge.
    ///
    /// Possible Outcomes:
    /// - FINANCIAL.TAX.CREATED: Configuration established.
    /// - FINANCIAL.TAX.VALIDATION_FAILED: Validation failed (e.g., negative rate).
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the established tax configuration.</response>
    /// <response code="400">Invalid parameters provided.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> CreateTax([FromBody] CreateTaxRequest request)
    {
        var command = new Application.Invoicing.Commands.CreateTaxConfiguration.CreateTaxConfigurationCommand(
            request.Name,
            request.Rate,
            request.IsInclusive,
            request.IsDefault);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/financial/taxes/{result.Value}", result, InvoicingOutcomes.Taxes.Created);
    }

    /// <summary>
    /// Formally removes a taxation configuration from the tenant's registry.
    /// </summary>
    /// <param name="id">Identifier of the configuration to remove.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - FINANCIAL.TAX.DELETED: Configuration successfully removed.
    /// - FINANCIAL.TAX.VALIDATION_FAILED: Configuration not found.
    /// </remarks>
    /// <response code="204">Configuration successfully removed.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> DeleteTax(Guid id)
    {
        var result = await Sender.Send(new Application.Invoicing.Commands.DeleteTaxConfiguration.DeleteTaxConfigurationCommand(id));
        return HandleDeleteResult(result, InvoicingOutcomes.Taxes.Deleted);
    }
}

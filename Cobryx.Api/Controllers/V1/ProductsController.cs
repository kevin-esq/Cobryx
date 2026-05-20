using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CreateProductCommand = Cobryx.Application.Products.Commands.Create.CreateProductCommand;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages the catalog of financial products and services.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Tags("Catalog")]
public class ProductsController(
    ISender sender,
    ITenantProvider tenantProvider,
    IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
    /// <summary>
    /// Creates a new product or service entry in the catalog.
    /// </summary>
    /// <param name="request">Product details including Price (decimal, 2-digit precision) and ISO-4217 Currency.</param>
    /// <remarks>
    /// Currency must be a 3-letter ISO-4217 code (e.g., MXN, USD).
    /// Pricing precision is enforced at the domain level.
    ///
    /// Possible Outcomes:
    /// - PRODUCT.CREATED: Product successfully established in the catalog.
    /// - PRODUCT.VALIDATION_FAILED: Validation failure (e.g., negative price).
    /// - PRODUCT.CONFLICT: A product with the same attributes already exists.
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the new product.</response>
    /// <response code="400">Invalid parameters, pricing, or currency code.</response>
    /// <response code="401">Authentication token missing or invalid.</response>
    /// <response code="409">Conflict: duplicate product entry detected.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        Guid? tenantId = tenantProvider.GetTenantId();

        if (tenantId is null)
            return Unauthorized();

        var price = new Money(request.Price, request.Currency);
        var command = new CreateProductCommand(tenantId.Value, request.Name, price, request.Description);

        Result<Guid> result = await Sender.Send(command);

        return HandleCreatedResult(linkGenerator.GetProductUrl(result.Value), result, ProductOutcomes.Created);
    }

    /// <summary>
    /// Retrieves a single product entry from the catalog by its identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the product.</param>
    /// <response code="200">Product record found.</response>
    /// <response code="404">Product not found.</response>
    /// <remarks>
    /// ⚠️ This endpoint is a stub. Full implementation via a dedicated query is pending.
    /// </remarks>
    [HttpGet("{id}", Name = "GetProduct")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetProduct(Guid id) =>
        Ok(ApiResponseFactory.Success(new { id, message = "Stub: product retrieval via dedicated query pending." }));
}

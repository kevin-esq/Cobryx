using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing the catalog of financial products and services.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Tags("Catalog")]
public class ProductsController(ISender sender, ITenantProvider tenantProvider, IApiLinkGenerator linkGenerator)
    : CobryxBaseController(sender)
{
    /// <summary>
    /// Creates a new product or service entry in the catalog.
    /// </summary>
    /// <param name="request">Product details including Price (Decimal, 2-digit) and ISO-4217 Currency.</param>
    /// <remarks>
    /// Currency must be a 3-letter ISO-4217 code (e.g., MXN, USD).
    /// Pricing precision is enforced at the domain level (decimal with 2 digits).
    ///
    /// Possible Outcomes:
    /// - PRODUCT.CREATED: Product successfully established in the catalog.
    /// - PRODUCT.VALIDATION_FAILED: Validation failure (e.g., negative price).
    /// - PRODUCT.CONFLICT: A product with similar attributes already exists.
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the new product.</response>
    /// <response code="400">Invalid parameters, pricing, or currency code.</response>
    /// <response code="401">Authentication token missing or invalid.</response>
    /// <response code="409">Conflict: Duplicate product entry detected.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (tenantId == null)
            return Unauthorized();

        var command = new Application.Products.Commands.Create.CreateProductCommand(
            tenantId.Value,
            request.Name,
            new Money(request.Price, request.Currency),
            request.Description);

        Result<Guid> result = await Sender.Send(command);
        return HandleCreatedResult(linkGenerator.GetProductUrl(result.Value), result, ProductOutcomes.Created);
    }

    /// <summary>
    /// Retrieves a single product entry from the catalog.
    /// </summary>
    [HttpGet("{id}", Name = "GetProduct")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public IActionResult GetProduct(Guid id) =>
        Ok(ApiResponseFactory.Success(new
            { id, message = "Product retrieval implemented via catalog search currently." }));
}

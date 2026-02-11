using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Financial;
using Cobryx.Api.Outcomes;
using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for managing the catalog of financial products and services.
/// </summary>
[Authorize]
[ApiController]
[Route("api/financial/products")]
[Tags("Financial Core")]
public class ProductsController : CobryxBaseController
{
    private readonly Application.Common.Interfaces.ITenantProvider _tenantProvider;

    public ProductsController(ISender sender, Application.Common.Interfaces.ITenantProvider tenantProvider) : base(sender)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Creates a new product or service entry in the catalog.
    /// </summary>
    /// <param name="request">Product details including Price (Decimal, 2-digit) and ISO-4217 Currency.</param>
    /// <remarks>
    /// Currency must be a 3-letter ISO code.
    ///
    /// Possible Outcomes:
    /// - PRODUCT.CREATED: Product successfully established.
    /// - PRODUCT.VALIDATION_FAILED: Validation failure.
    /// - PRODUCT.CONFLICT: Duplicate code.
    /// </remarks>
    /// <response code="201">Returns the identifier for the new product.</response>
    /// <response code="400">Invalid pricing or currency.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="409">Duplicate product code.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Intentional Mapping: Public Request -> Internal Domain Value Objects
        var command = new Application.Products.Commands.Create.CreateProductCommand(
            tenantId.Value,
            request.Name,
            new Money(request.Price, request.Currency),
            request.Description);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/financial/products/{result.Value}", result, ProductOutcomes.Created);
    }
}

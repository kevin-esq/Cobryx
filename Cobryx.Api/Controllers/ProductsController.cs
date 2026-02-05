using Cobryx.Application.Products.Queries.GetProducts;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cobryx.Api.Outcomes;

namespace Cobryx.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : CobryxBaseController
{
    public ProductsController(ISender sender) : base(sender)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new GetProductsQuery(page, pageSize));
        return HandleResult(result, "PRODUCT.SEARCH.COMPLETED");
    }
}

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Application.Products.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Products.Queries.GetProducts;

[TenantScoped]
public record GetProductsQuery(int Page = 1, int PageSize = 10) : IRequest<Result<PaginatedList<ProductDto>>>, IRequiresTenant;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<PaginatedList<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetProductsHandler(IProductRepository productRepository, ITenantProvider tenantProvider)
    {
        _productRepository = productRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PaginatedList<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<PaginatedList<ProductDto>>(DomainErrorCode.Tenant.ContextMissing);

        var allProducts = await _productRepository.GetAllAsync(cancellationToken);
        var products = allProducts.Where(p => p.TenantId == tenantId.Value);

        var totalCount = products.Count();
        var items = products
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description ?? string.Empty,
                p.BasePrice.Amount,
                p.BasePrice.Currency,
                p.DefaultInterestRate,
                p.MaxInstallments))
            .ToList();

        return Result.Success(new PaginatedList<ProductDto>(
            items,
            totalCount,
            request.Page,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }
}

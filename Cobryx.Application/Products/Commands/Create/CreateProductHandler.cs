using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Products.Commands.Create;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly ITenantProvider _tenantProvider;

    public CreateProductHandler(IProductRepository productRepository, ITenantProvider tenantProvider)
    {
        _productRepository = productRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
        {
            return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
        }

        var product = new Product(
            tenantId.Value,
            request.Name,
            request.BasePrice,
            request.IsService,
            request.IsLoanProduct);

        if (request.IsLoanProduct)
        {
            product.ConfigureLoanRules(request.DefaultInterestRate, request.MaxInstallments);
        }

        await _productRepository.AddAsync(product);

        return Result.Success(product.Id);
    }
}

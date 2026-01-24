using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Products.Commands.Create;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;

    public CreateProductHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(
            request.TenantId,
            request.Name,
            request.BasePrice,
            request.IsService,
            request.IsLoanProduct);

        if (request.IsLoanProduct)
        {
            product.ConfigureLoanRules(request.DefaultInterestRate, request.MaxInstallments);
        }

        await _repository.AddAsync(product);
        
        return Result.Success(product.Id);
    }
}

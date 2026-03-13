using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Products.Commands.Create;

public record CreateProductCommand(
    Guid TenantId,
    string Name,
    Money BasePrice,
    string? Description = null,
    string? Sku = null,
    bool IsService = false,
    bool IsLoanProduct = false,
    decimal? DefaultInterestRate = null,
    int? MaxInstallments = null
) : IRequest<Result<Guid>>;

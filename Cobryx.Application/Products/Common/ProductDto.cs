namespace Cobryx.Application.Products.Common;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal BasePrice,
    string Currency,
    decimal? DefaultInterestRate,
    int? MaxInstallments);

using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.ValueObjects;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Products.Commands.Create;

[TenantScoped]
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
) : IRequest<Result<Guid>>, IRequiresTenant;

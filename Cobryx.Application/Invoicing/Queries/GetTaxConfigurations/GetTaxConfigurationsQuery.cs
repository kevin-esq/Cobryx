using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Invoicing.Queries.GetTaxConfigurations;

public record GetTaxConfigurationsQuery() : IRequest<Result<IReadOnlyList<TaxDto>>>;

public record TaxDto(Guid Id, string Name, decimal Rate, bool IsInclusive, bool IsDefault);

public class GetTaxConfigurationsHandler : IRequestHandler<GetTaxConfigurationsQuery, Result<IReadOnlyList<TaxDto>>>
{
    private readonly ITaxConfigurationRepository _taxRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetTaxConfigurationsHandler(
        ITaxConfigurationRepository taxRepository,
        ITenantProvider tenantProvider)
    {
        _taxRepository = taxRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<TaxDto>>> Handle(GetTaxConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<IReadOnlyList<TaxDto>>("Tenant context missing.");

        var taxes = await _taxRepository.GetAllActiveAsync(tenantId.Value);

        var dtos = taxes.Select(t => new TaxDto(
            t.Id,
            t.Name,
            t.Rate,
            t.IsInclusive,
            t.IsDefault
        )).ToList();

        return Result.Success<IReadOnlyList<TaxDto>>(dtos);
    }
}

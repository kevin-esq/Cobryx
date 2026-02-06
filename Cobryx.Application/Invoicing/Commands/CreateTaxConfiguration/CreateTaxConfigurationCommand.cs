using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Invoicing.Commands.CreateTaxConfiguration;

public record CreateTaxConfigurationCommand(
    string Name,
    decimal Rate,
    bool IsInclusive = true,
    bool IsDefault = false) : IRequest<Result<Guid>>;

public class CreateTaxConfigurationHandler : IRequestHandler<CreateTaxConfigurationCommand, Result<Guid>>
{
    private readonly ITaxConfigurationRepository _taxRepository;
    private readonly ITenantProvider _tenantProvider;

    public CreateTaxConfigurationHandler(
        ITaxConfigurationRepository taxRepository,
        ITenantProvider tenantProvider)
    {
        _taxRepository = taxRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(CreateTaxConfigurationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<Guid>("Tenant context missing.");

        if (request.IsDefault)
        {
            var existingDefault = await _taxRepository.GetDefaultAsync(tenantId.Value);
            if (existingDefault != null)
            {
                existingDefault.UnsetDefault();
                await _taxRepository.UpdateAsync(existingDefault);
            }
        }

        var tax = new TaxConfiguration(
            tenantId.Value,
            request.Name,
            request.Rate,
            request.IsInclusive,
            request.IsDefault);

        await _taxRepository.AddAsync(tax);

        return Result.Success(tax.Id);
    }
}

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.CreateTaxConfiguration;

public record CreateTaxConfigurationCommand(
    string Name,
    decimal Rate,
    bool IsInclusive = true,
    bool IsDefault = false) : IRequest<Result<Guid>>;

public class CreateTaxConfigurationHandler(
    ITaxConfigurationRepository taxRepository,
    ITenantProvider tenantProvider) : IRequestHandler<CreateTaxConfigurationCommand, Result<Guid>>
{
    private readonly ITaxConfigurationRepository _taxRepository = taxRepository;
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    public async Task<Result<Guid>> Handle(CreateTaxConfigurationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);

        if (request.IsDefault)
        {
            var existingDefault = await _taxRepository.GetDefaultAsync(tenantId.Value, cancellationToken);
            if (existingDefault != null)
            {
                existingDefault.UnsetDefault();
                await _taxRepository.UpdateAsync(existingDefault, cancellationToken);
            }
        }

        var tax = new TaxConfiguration(
            tenantId.Value,
            request.Name,
            request.Rate,
            request.IsInclusive,
            request.IsDefault);

        await _taxRepository.AddAsync(tax, cancellationToken);

        return Result.Success(tax.Id);
    }
}

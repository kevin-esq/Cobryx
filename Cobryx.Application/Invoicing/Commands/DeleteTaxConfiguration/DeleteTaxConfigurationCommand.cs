using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.DeleteTaxConfiguration;

public record DeleteTaxConfigurationCommand(Guid Id) : IRequest<Result>;

public class DeleteTaxConfigurationHandler : IRequestHandler<DeleteTaxConfigurationCommand, Result>
{
    private readonly ITaxConfigurationRepository _taxRepository;
    private readonly ITenantProvider _tenantProvider;

    public DeleteTaxConfigurationHandler(
        ITaxConfigurationRepository taxRepository,
        ITenantProvider tenantProvider)
    {
        _taxRepository = taxRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(DeleteTaxConfigurationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var tax = await _taxRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tax == null || tax.TenantId != tenantId.Value)
            return Result.Failure(DomainErrorCode.Invoicing.TaxNotFound);

        if (tax.IsDefault)
            return Result.Failure(DomainErrorCode.Invoicing.TaxDeleteDefaultForbidden);

        tax.Delete();
        await _taxRepository.UpdateAsync(tax, cancellationToken);

        return Result.Success();
    }
}

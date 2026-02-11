using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

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
        if (!tenantId.HasValue) return Result.Failure("TENANT.CONTEXT_MISSING");

        var tax = await _taxRepository.GetByIdAsync(request.Id);
        if (tax == null || tax.TenantId != tenantId.Value)
            return Result.Failure("TAX.NOT_FOUND");

        if (tax.IsDefault)
            return Result.Failure("TAX.DELETE_DEFAULT_FORBIDDEN");

        tax.Delete();
        await _taxRepository.UpdateAsync(tax);

        return Result.Success();
    }
}

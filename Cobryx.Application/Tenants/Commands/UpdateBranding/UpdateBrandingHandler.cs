using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Tenants.Commands.UpdateBranding;

public class UpdateBrandingHandler : IRequestHandler<UpdateBrandingCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvider _tenantProvider;

    public UpdateBrandingHandler(ITenantRepository tenantRepository, ITenantProvider tenantProvider)
    {
        _tenantRepository = tenantRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
        {
            return Result.Failure("TENANT.CONTEXT_MISSING");
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value);
        if (tenant == null)
        {
            return Result.Failure("TENANT.NOT_FOUND");
        }

        tenant.UpdateBranding(request.LogoUrl, request.PrimaryColor, request.SecondaryColor);
        await _tenantRepository.UpdateAsync(tenant);

        return Result.Success();
    }
}

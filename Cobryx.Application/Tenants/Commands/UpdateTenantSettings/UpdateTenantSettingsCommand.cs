using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Tenants.Commands.UpdateTenantSettings;

public record UpdateTenantSettingsCommand(
    string? OwnerName,
    string? Phone,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor) : IRequest<Result>;

public class UpdateTenantSettingsHandler : IRequestHandler<UpdateTenantSettingsCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantSettingsHandler(
        ITenantRepository tenantRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null) return Result.Failure(DomainErrorCode.Tenant.NotFound);

        tenant.UpdateBranding(request.LogoUrl, request.PrimaryColor, request.SecondaryColor);
        tenant.UpdateContactInfo(request.OwnerName, request.Phone);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

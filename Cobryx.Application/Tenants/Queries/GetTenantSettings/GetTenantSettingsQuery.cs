using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Tenants.Queries.GetTenantSettings;

public record TenantSettingsDto(
    Guid Id,
    string BusinessName,
    string? OwnerName,
    string? Phone,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string Currency,
    string? TaxId,
    string? Industry,
    string? BusinessAddress,
    bool IsActive,
    string OnboardingStatus);

public record GetTenantSettingsQuery : IRequest<Result<TenantSettingsDto>>;

public class GetTenantSettingsHandler : IRequestHandler<GetTenantSettingsQuery, Result<TenantSettingsDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetTenantSettingsHandler(ITenantRepository tenantRepository, ITenantProvider tenantProvider)
    {
        _tenantRepository = tenantRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TenantSettingsDto>> Handle(GetTenantSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<TenantSettingsDto>("Tenant context missing.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null) return Result.Failure<TenantSettingsDto>(DomainErrorCode.Tenant.NotFound);

        return Result.Success(new TenantSettingsDto(
            tenant.Id,
            tenant.BusinessName,
            tenant.OwnerName,
            tenant.Phone,
            tenant.LogoUrl,
            tenant.PrimaryColor,
            tenant.SecondaryColor,
            tenant.Currency,
            tenant.TaxId?.Value,
            tenant.Industry,
            tenant.BusinessAddress,
            tenant.IsActive,
            tenant.OnboardingStatus.ToString()));
    }
}

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
    string OnboardingStatus
)
{
    /// <summary>Unique identifier for the tenant.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Display name for the business.</summary>
    /// <example>Acme Corp</example>
    public string BusinessName { get; init; } = BusinessName;

    /// <summary>Full name of the legal owner.</summary>
    /// <example>Jane Smith</example>
    public string? OwnerName { get; init; } = OwnerName;

    /// <summary>Business contact phone number.</summary>
    /// <example>+525512345678</example>
    public string? Phone { get; init; } = Phone;

    /// <summary>Public URL for the brand logo.</summary>
    /// <example>https://storage.cobryx.mx/logos/acme_logo.png</example>
    public string? LogoUrl { get; init; } = LogoUrl;

    /// <summary>Hex code for primary branding color.</summary>
    /// <example>#1a73e8</example>
    public string? PrimaryColor { get; init; } = PrimaryColor;

    /// <summary>Hex code for secondary branding color.</summary>
    /// <example>#5f6368</example>
    public string? SecondaryColor { get; init; } = SecondaryColor;

    /// <summary>Default currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>Legal tax identification number.</summary>
    /// <example>RFC123456789</example>
    public string? TaxId { get; init; } = TaxId;

    /// <summary>Business vertical or industry type.</summary>
    /// <example>Technology</example>
    public string? Industry { get; init; } = Industry;

    /// <summary>Legal registered address.</summary>
    /// <example>Av. Insurgentes Sur 123, CDMX</example>
    public string? BusinessAddress { get; init; } = BusinessAddress;

    /// <summary>Current account operational status.</summary>
    /// <example>true</example>
    public bool IsActive { get; init; } = IsActive;

    /// <summary>Current progress in the onboarding flow.</summary>
    /// <example>COMPLETED</example>
    public string OnboardingStatus { get; init; } = OnboardingStatus;
}

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
        if (!tenantId.HasValue) return Result.Failure<TenantSettingsDto>(DomainErrorCode.Tenant.ContextMissing);

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

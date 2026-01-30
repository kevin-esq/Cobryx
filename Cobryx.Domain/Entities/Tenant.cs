using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Tenant : BaseEntity, IAggregateRoot
{
    public string BusinessName { get; private set; }
    public string? OwnerName { get; private set; }
    public string? Phone { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string? SecondaryColor { get; private set; }
    public string Currency { get; private set; }
    public TaxId? TaxId { get; private set; }
    public string? Industry { get; private set; }
    public string? BusinessAddress { get; private set; }
    public bool IsActive { get; private set; }
    public BusinessSettings Settings { get; private set; }

    private Tenant()
    {
        BusinessName = null!;
        Currency = null!;
        Settings = null!;
    }

    public Tenant(string businessName, string currency = "MXN")
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name cannot be empty.", nameof(businessName));

        BusinessName = businessName;
        Currency = currency;
        IsActive = true;
        Settings = BusinessSettings.Default();
    }

    public static Tenant CreateForRegistration(string businessName, string? taxIdCode, string? industry, string? address)
    {
        var tenant = new Tenant(businessName);
        if (!string.IsNullOrEmpty(taxIdCode)) tenant.TaxId = new TaxId(taxIdCode);
        tenant.Industry = industry;
        tenant.BusinessAddress = address;
        return tenant;
    }


    public void UpdateBranding(string? logoUrl, string? primaryColor, string? secondaryColor)
    {
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        UpdateTimestamp();
    }

    public void UpdateContactInfo(string? ownerName, string? phone)
    {
        OwnerName = ownerName;
        Phone = phone;
        UpdateTimestamp();
    }
}

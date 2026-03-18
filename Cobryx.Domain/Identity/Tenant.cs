using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Identity;

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
    public TenantStatus Status { get; private set; }
    public bool FinancialSafeMode { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public TenantOnboardingStatus OnboardingStatus { get; private set; }
    public string? StripeAccountId { get; private set; }
    public TenantConnectCapability ConnectCapabilities { get; private set; } = TenantConnectCapability.NotStarted();
    public BusinessSettings Settings { get; private set; }
    public TenantGrowthMetrics? GrowthMetrics { get; private set; }

    private Tenant()
    {
        BusinessName = null!;
        Currency = null!;
        Settings = null!;
    }

    public Tenant(string businessName, string currency = CobryxDefaults.Currency)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new DomainException(DomainErrorCode.Tenant.BusinessNameRequired);

        BusinessName = businessName;
        Currency = currency;
        IsActive = true;
        Status = TenantStatus.Active;
        OnboardingStatus = TenantOnboardingStatus.Pending;
        Settings = BusinessSettings.Default();
    }

    public static Tenant CreateForRegistration(string businessName, string? taxIdCode, string? industry, string? address)
    {
        var tenant = new Tenant(businessName);
        if (!string.IsNullOrEmpty(taxIdCode))
            tenant.TaxId = new TaxId(taxIdCode);
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

    public void UpdateSettings(BusinessSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdateTimestamp();
    }

    public void UpdateOnboardingInfo(string taxIdCode, string industry, string address, string? phone = null)
    {
        TaxId = new TaxId(taxIdCode);
        Industry = industry;
        BusinessAddress = address;
        if (!string.IsNullOrEmpty(phone))
            Phone = phone;
        OnboardingStatus = TenantOnboardingStatus.Completed;
        UpdateTimestamp();
    }

    public void TriggerOnboardingMilestone(string milestoneCode)
    {
        AddDomainEvent(new Cobryx.Domain.Events.Onboarding.OnboardingMilestoneReachedEvent(
            Id,
            milestoneCode,
            DateTime.UtcNow));
    }

    public void SetStripeAccountId(string stripeAccountId)
    {
        if (string.IsNullOrWhiteSpace(stripeAccountId))
            throw new ArgumentException("StripeAccountId cannot be empty", nameof(stripeAccountId));

        StripeAccountId = stripeAccountId;
        UpdateTimestamp();
    }

    public void UpdateConnectStatus(bool charges, bool payouts, bool detailsSubmitted)
    {
        ConnectCapabilities = new TenantConnectCapability(charges, payouts, detailsSubmitted);
        UpdateTimestamp();
    }

    public bool IsConnectActive =>
        !string.IsNullOrEmpty(StripeAccountId) &&
        ConnectCapabilities.ChargesEnabled &&
        ConnectCapabilities.PayoutsEnabled &&
        ConnectCapabilities.DetailsSubmitted;

    /// <summary>
    /// Financial Guard Rail: If critical requirements are missing in Stripe,
    /// we restrict new financial operations to prevent failed UX.
    /// </summary>
    public bool IsPaymentRestricted =>
        IsSuspended ||
        string.IsNullOrEmpty(StripeAccountId) ||
        !ConnectCapabilities.ChargesEnabled ||
        !ConnectCapabilities.DetailsSubmitted;

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        UpdateTimestamp();
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        UpdateTimestamp();
    }

    public bool IsSuspended => Status == TenantStatus.Suspended;

    public void ToggleFinancialSafeMode(bool enabled)
    {
        FinancialSafeMode = enabled;
        UpdateTimestamp();
    }
}

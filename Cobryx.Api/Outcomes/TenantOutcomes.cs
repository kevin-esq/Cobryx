using Cobryx.Domain.Common;

namespace Cobryx.Api.Outcomes;

public static class TenantOutcomes
{
    private const string Prefix = "TENANT.ADMIN";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "Tenant created successfully.");
    public static readonly Outcome Updated = new($"{Prefix}.UPDATED_SUCCESS", OutcomeCategory.Success, "Tenant updated successfully.");
    public static readonly Outcome BrandingUpdated = new($"{Prefix}.BRANDING_UPDATE_SUCCESS", OutcomeCategory.Success, "Tenant branding updated successfully.");
    public static readonly Outcome SettingsUpdated = new($"{Prefix}.SETTINGS_UPDATE_SUCCESS", OutcomeCategory.Success, "Tenant settings updated successfully.");
    public static readonly Outcome OnboardingCompleted = new($"{Prefix}.ONBOARDING_SUCCESS", OutcomeCategory.Success, "Tenant onboarding completed.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Tenant search completed.");
}

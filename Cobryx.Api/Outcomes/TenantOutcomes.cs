namespace Cobryx.Api.Outcomes;

public static class TenantOutcomes
{
    private const string Prefix = "TENANT.ADMIN";

    public const string BrandingUpdated = $"{Prefix}.BRANDING_UPDATE_SUCCESS";
    public const string SettingsUpdated = $"{Prefix}.SETTINGS_UPDATE_SUCCESS";
    public const string OnboardingCompleted = $"{Prefix}.ONBOARDING_SUCCESS";
    public const string SearchCompleted = $"{Prefix}.SEARCH_SUCCESS";
}

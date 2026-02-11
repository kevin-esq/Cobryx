namespace Cobryx.Api.Outcomes;

public static class TenantOutcomes
{
    private const string Prefix = "TENANT";

    public const string BrandingUpdated = $"{Prefix}.BRANDING.UPDATED";
    public const string SettingsUpdated = $"{Prefix}.SETTINGS.UPDATED";
    public const string OnboardingCompleted = $"{Prefix}.ONBOARDING.COMPLETED";
    public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
}

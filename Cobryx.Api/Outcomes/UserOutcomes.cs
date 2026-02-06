namespace Cobryx.Api.Outcomes;

public static class UserOutcomes
{
    private const string Prefix = "USER";

    public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
    public const string ProfileUpdated = $"{Prefix}.PROFILE.UPDATED";
    public const string PasswordChanged = $"{Prefix}.PASSWORD.CHANGED";
}

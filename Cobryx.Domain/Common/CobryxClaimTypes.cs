namespace Cobryx.Domain.Common;

/// <summary>
/// Custom JWT claim type names used in Cobryx tokens.
/// </summary>
public static class CobryxClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string TenantName = "tenant_name";
    public const string SessionId = "sid";
    public const string Permissions = "permissions";
    public const string RequiresOnboarding = "requires_onboarding";
    public const string EmailVerified = "email_verified";
    public const string Purpose = "purpose";
    public const string MfaVerification = "mfa_verification";
    public const string AccessTokenCookieName = "X-Access-Token";
}

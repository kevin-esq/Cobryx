namespace Cobryx.Api.Outcomes;

public static class AuthOutcomes
{
    public const string LoginCompleted = "AUTH.LOGIN.COMPLETED";
    public const string LoginMfaRequired = "AUTH.LOGIN.MFA_REQUIRED";
    public const string SignupCreated = "AUTH.SIGNUP.CREATED";
    public const string SignupVerificationRequired = "AUTH.SIGNUP.VERIFICATION_REQUIRED";
    public const string VerificationEmailSent = "AUTH.VERIFICATION_EMAIL_SENT";
    public const string EmailVerified = "AUTH.EMAIL_VERIFIED";
    public const string PasswordResetRequested = "AUTH.PASSWORD_RESET_REQUESTED";
    public const string PasswordChanged = "AUTH.PASSWORD_CHANGED";
    public const string MfaEnabled = "AUTH.MFA.ENABLED";
    public const string MfaVerified = "AUTH.MFA.VERIFIED";
    public const string Fido2Registered = "AUTH.FIDO2.REGISTERED";
    public const string SessionRevoked = "AUTH.SESSION.REVOKED";
    public const string SessionSearchCompleted = "AUTH.SESSION.SEARCH.COMPLETED";
    public const string MfaInitiated = "AUTH.MFA.INITIATED";
    public const string LogoutCompleted = "AUTH.LOGOUT.COMPLETED";
    public const string LogoutAllCompleted = "AUTH.LOGOUT_ALL.COMPLETED";
    public const string TokenRotated = "AUTH.TOKEN.ROTATED";
}

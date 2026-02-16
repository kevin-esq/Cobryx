namespace Cobryx.Api.Outcomes;

public static class AuthOutcomes
{
    public const string LoginCompleted = "AUTH.USER.LOGIN_SUCCESS";
    public const string LoginMfaRequired = "AUTH.USER.LOGIN_MFA_REQUIRED";
    public const string SignupCreated = "AUTH.USER.SIGNUP_SUCCESS";
    public const string SignupVerificationRequired = "AUTH.USER.VERIFICATION_REQUIRED";
    public const string VerificationEmailSent = "AUTH.EMAIL.SENT_SUCCESS";
    public const string EmailVerified = "AUTH.EMAIL.VERIFIED_SUCCESS";
    public const string PasswordResetRequested = "AUTH.PASSWORD.RESET_REQUESTED";
    public const string PasswordResetEmailSent = "AUTH.PASSWORD.EMAIL_SENT_SUCCESS";
    public const string PasswordChanged = "AUTH.PASSWORD.CHANGED_SUCCESS";
    public const string MfaEnabled = "AUTH.MFA.ENABLED_SUCCESS";
    public const string MfaVerified = "AUTH.MFA.VERIFIED_SUCCESS";
    public const string Fido2Registered = "AUTH.FIDO2.REGISTERED_SUCCESS";
    public const string SessionRevoked = "AUTH.SESSION.REVOKED_SUCCESS";
    public const string SessionSearchCompleted = "AUTH.SESSION.SEARCH_SUCCESS";
    public const string MfaInitiated = "AUTH.MFA.INITIATED_SUCCESS";
    public const string LogoutCompleted = "AUTH.USER.LOGOUT_SUCCESS";
    public const string LogoutAllCompleted = "AUTH.USER.LOGOUT_ALL_SUCCESS";
    public const string TokenRotated = "AUTH.TOKEN.ROTATED_SUCCESS";
    public const string UnknownError = "AUTH.SYSTEM.UNKNOWN_ERROR";
    public const string Fido2InvalidPayload = "AUTH.FIDO2.INVALID_PAYLOAD";
}

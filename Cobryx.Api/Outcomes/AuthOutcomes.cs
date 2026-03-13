using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class AuthOutcomes
{
    private const string Prefix = "AUTH";

    public static readonly Outcome LoginCompleted = new($"{Prefix}.USER.LOGIN_SUCCESS", OutcomeCategory.Success, "Login successful.");
    public static readonly Outcome LoginMfaRequired = new($"{Prefix}.USER.LOGIN_MFA_REQUIRED", OutcomeCategory.Info, "MFA verification required.");
    public static readonly Outcome SignupCreated = new($"{Prefix}.USER.SIGNUP_SUCCESS", OutcomeCategory.Success, "Signup successful.");
    public static readonly Outcome SignupVerificationRequired = new($"{Prefix}.USER.VERIFICATION_REQUIRED", OutcomeCategory.Info, "Verification required.");
    public static readonly Outcome VerificationEmailSent = new($"{Prefix}.EMAIL.SENT_SUCCESS", OutcomeCategory.Success, "Verification email sent.");
    public static readonly Outcome EmailVerified = new($"{Prefix}.EMAIL.VERIFIED_SUCCESS", OutcomeCategory.Success, "Email verified successfully.");
    public static readonly Outcome PasswordResetRequested = new($"{Prefix}.PASSWORD.RESET_REQUESTED", OutcomeCategory.Success, "Password reset requested.");
    public static readonly Outcome PasswordResetEmailSent = new($"{Prefix}.PASSWORD.EMAIL_SENT_SUCCESS", OutcomeCategory.Success, "Password reset email sent.");
    public static readonly Outcome PasswordChanged = new($"{Prefix}.PASSWORD.CHANGED_SUCCESS", OutcomeCategory.Success, "Password changed successfully.");
    public static readonly Outcome MfaEnabled = new($"{Prefix}.MFA.ENABLED_SUCCESS", OutcomeCategory.Success, "MFA enabled successfully.");
    public static readonly Outcome MfaVerified = new($"{Prefix}.MFA.VERIFIED_SUCCESS", OutcomeCategory.Success, "MFA verified successfully.");
    public static readonly Outcome Fido2Registered = new($"{Prefix}.FIDO2.REGISTERED_SUCCESS", OutcomeCategory.Success, "FIDO2 registered successfully.");
    public static readonly Outcome SessionRevoked = new($"{Prefix}.SESSION.REVOKED_SUCCESS", OutcomeCategory.Success, "Session revoked successfully.");
    public static readonly Outcome SessionSearchCompleted = new($"{Prefix}.SESSION.SEARCH_SUCCESS", OutcomeCategory.Success, "Session search completed.");
    public static readonly Outcome MfaInitiated = new($"{Prefix}.MFA.INITIATED_SUCCESS", OutcomeCategory.Success, "MFA initiated successfully.");

    public static readonly Outcome LogoutCompleted = new($"{Prefix}.USER.LOGOUT_SUCCESS", OutcomeCategory.Success, "Logout successful.");
    public static readonly Outcome LogoutAllCompleted = new($"{Prefix}.USER.LOGOUT_ALL_SUCCESS", OutcomeCategory.Success, "Logout from all sessions successful.");
    public static readonly Outcome TokenRotated = new($"{Prefix}.TOKEN.ROTATED_SUCCESS", OutcomeCategory.Success, "Token rotated successfully.");

    public static readonly Outcome UnknownError = new($"{Prefix}.SYSTEM.UNKNOWN_ERROR", OutcomeCategory.Critical, "An unknown error occurred.");
    public static readonly Outcome Fido2InvalidPayload = new($"{Prefix}.FIDO2.INVALID_PAYLOAD", OutcomeCategory.BusinessError, "Invalid FIDO2 payload.");
}

namespace Cobryx.Api.Outcomes;

public static class AuthOutcomes
{
    public const string LoginSuccess = "AUTH.LOGIN.SUCCESS";
    public const string LoginMfaRequired = "AUTH.LOGIN.MFA_REQUIRED";
    public const string SignupCompleted = "AUTH.SIGNUP.COMPLETED";
    public const string SignupVerificationRequired = "AUTH.SIGNUP.VERIFICATION_REQUIRED";
    public const string VerificationEmailSent = "AUTH.VERIFICATION_EMAIL_SENT";
    public const string VerificationEmailVerified = "AUTH.EMAIL_VERIFIED";
}

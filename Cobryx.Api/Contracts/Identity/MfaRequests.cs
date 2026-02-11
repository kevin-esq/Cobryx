namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for enabling MFA (TOTP).
/// </summary>
public record EnableMfaRequest(string Code, string Secret);

/// <summary>
/// Public contract for verifying an MFA code during login.
/// </summary>
public record VerifyMfaRequest(string Code, string PersistenceToken);

/// <summary>
/// Public contract for completing FIDO2 registration.
/// </summary>
public record CompleteFido2RegistrationRequest(
    string DeviceName,
    PasskeyRegistrationData RegistrationData,
    PasskeyRegistrationChallenge Challenge
);

/// <summary>
/// Public contract for initiating FIDO2 verification (authentication).
/// </summary>
public record InitiateFido2AssertionRequest(string PersistenceToken);

/// <summary>
/// Public contract for completing FIDO2 verification (authentication).
/// </summary>
public record CompleteFido2AssertionRequest(
    string PersistenceToken,
    PasskeyVerificationData VerificationData,
    PasskeyVerificationChallenge Challenge
);

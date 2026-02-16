namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for enabling MFA (TOTP).
/// </summary>
public record EnableMfaRequest(string Code, string Secret)
{
    /// <summary>The 6-digit verification code from the authenticator app.</summary>
    /// <example>123456</example>
    public string Code { get; init; } = Code;

    /// <summary>The base32 secret shared with the authenticator app.</summary>
    /// <example>JBSWY3DPEHPK3PXP</example>
    public string Secret { get; init; } = Secret;
}

/// <summary>
/// Public contract for verifying an MFA code during login.
/// </summary>
public record VerifyMfaRequest(string Code, string PersistenceToken)
{
    /// <summary>The 6-digit verification code from the authenticator app.</summary>
    /// <example>123456</example>
    public string Code { get; init; } = Code;

    /// <summary>Short-lived persistence token from the initial login attempt.</summary>
    /// <example>pt_7f2b9a...</example>
    public string PersistenceToken { get; init; } = PersistenceToken;
}

/// <summary>
/// Public contract for completing FIDO2 registration.
/// </summary>
public record CompleteFido2RegistrationRequest(
    string DeviceName,
    PasskeyRegistrationData RegistrationData,
    PasskeyRegistrationChallenge Challenge
)
{
    /// <summary>User-friendly name for the security key.</summary>
    /// <example>My YubiKey 5C</example>
    public string DeviceName { get; init; } = DeviceName;

    /// <summary>Device-generated registration data.</summary>
    public PasskeyRegistrationData RegistrationData { get; init; } = RegistrationData;

    /// <summary>Original challenge and options sent to the device.</summary>
    public PasskeyRegistrationChallenge Challenge { get; init; } = Challenge;
}

/// <summary>
/// Public contract for initiating FIDO2 verification (authentication).
/// </summary>
public record InitiateFido2AssertionRequest(string PersistenceToken)
{
    /// <summary>Short-lived persistence token from the initial login attempt.</summary>
    /// <example>pt_7f2b9a...</example>
    public string PersistenceToken { get; init; } = PersistenceToken;
}

/// <summary>
/// Public contract for completing FIDO2 verification (authentication).
/// </summary>
public record CompleteFido2AssertionRequest(
    string PersistenceToken,
    PasskeyVerificationData VerificationData,
    PasskeyVerificationChallenge Challenge
)
{
    /// <summary>Short-lived persistence token from the initial login attempt.</summary>
    /// <example>pt_7f2b9a...</example>
    public string PersistenceToken { get; init; } = PersistenceToken;

    /// <summary>Device-generated verification signature data.</summary>
    public PasskeyVerificationData VerificationData { get; init; } = VerificationData;

    /// <summary>Original challenge and options sent to the device.</summary>
    public PasskeyVerificationChallenge Challenge { get; init; } = Challenge;
}

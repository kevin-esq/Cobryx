using System.Text.Json;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Data required to register a new passkey (FIDO2/WebAuthn).
/// </summary>
public record PasskeyRegistrationData(
    string Id,
    string RawId,
    string Type,
    JsonElement Response,
    JsonElement Extensions
);

/// <summary>
/// Data required to verify a passkey signature during login.
/// </summary>
public record PasskeyVerificationData(
    string Id,
    string RawId,
    string Type,
    JsonElement Response,
    JsonElement Extensions
);

/// <summary>
/// Challenge and options sent to the device to initiate passkey registration.
/// </summary>
public record PasskeyRegistrationChallenge(
    string Status,
    JsonElement Options
);

/// <summary>
/// Challenge and options sent to the device to initiate passkey verification.
/// </summary>
public record PasskeyVerificationChallenge(
    string Status,
    JsonElement Options
);

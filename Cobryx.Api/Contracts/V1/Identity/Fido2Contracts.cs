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
)
{
    /// <summary>Base64url-encoded credential ID.</summary>
    /// <example>AQIDBAUGBwgJCgsMDQ4PEA==</example>
    public string Id { get; init; } = Id;

    /// <summary>Raw byte representation of the ID (base64url).</summary>
    /// <example>AQIDBAUGBwgJCgsMDQ4PEA==</example>
    public string RawId { get; init; } = RawId;

    /// <summary>Credential type (always "public-key").</summary>
    /// <example>public-key</example>
    public string Type { get; init; } = Type;

    /// <summary>Authenticator attestation response data.</summary>
    public JsonElement Response { get; init; } = Response;

    /// <summary>Optional client extension results.</summary>
    public JsonElement Extensions { get; init; } = Extensions;
}

/// <summary>
/// Data required to verify a passkey signature during login.
/// </summary>
public record PasskeyVerificationData(
    string Id,
    string RawId,
    string Type,
    JsonElement Response,
    JsonElement Extensions
)
{
    /// <summary>Base64url-encoded credential ID.</summary>
    /// <example>AQIDBAUGBwgJCgsMDQ4PEA==</example>
    public string Id { get; init; } = Id;

    /// <summary>Raw byte representation of the ID (base64url).</summary>
    /// <example>AQIDBAUGBwgJCgsMDQ4PEA==</example>
    public string RawId { get; init; } = RawId;

    /// <summary>Credential type (always "public-key").</summary>
    /// <example>public-key</example>
    public string Type { get; init; } = Type;

    /// <summary>Authenticator assertion response data (signature, clientDataJSON).</summary>
    public JsonElement Response { get; init; } = Response;

    /// <summary>Optional client extension results.</summary>
    public JsonElement Extensions { get; init; } = Extensions;
}

/// <summary>
/// Challenge and options sent to the device to initiate passkey registration.
/// </summary>
public record PasskeyRegistrationChallenge(
    string Status,
    JsonElement Options
)
{
    /// <summary>Status of the challenge generation.</summary>
    /// <example>OK</example>
    public string Status { get; init; } = Status;

    /// <summary>JSON serialized CredentialCreateOptions for the browser.</summary>
    public JsonElement Options { get; init; } = Options;
}

/// <summary>
/// Challenge and options sent to the device to initiate passkey verification.
/// </summary>
public record PasskeyVerificationChallenge(
    string Status,
    JsonElement Options
)
{
    /// <summary>Status of the challenge generation.</summary>
    /// <example>OK</example>
    public string Status { get; init; } = Status;

    /// <summary>JSON serialized AssertionOptions for the browser.</summary>
    public JsonElement Options { get; init; } = Options;
}

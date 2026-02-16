namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for updating the authenticated user's profile.
/// </summary>
public record UpdateProfileRequest(
    string? PhoneNumber,
    string? AvatarUrl,
    string? PreferredLanguage,
    string? Timezone
)
{
    /// <summary>User's primary contact phone number.</summary>
    /// <example>+525512345678</example>
    public string? PhoneNumber { get; init; } = PhoneNumber;

    /// <summary>Public URL for the user's profile picture.</summary>
    /// <example>https://storage.cobryx.mx/avatars/user_123.jpg</example>
    public string? AvatarUrl { get; init; } = AvatarUrl;

    /// <summary>Preferred UI language (ISO 639-1).</summary>
    /// <example>es</example>
    public string? PreferredLanguage { get; init; } = PreferredLanguage;

    /// <summary>Preferred IANA timezone name.</summary>
    /// <example>America/Mexico_City</example>
    public string? Timezone { get; init; } = Timezone;
}

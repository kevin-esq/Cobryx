namespace Cobryx.Application.Users.Common;

/// <summary>
/// Full user profile projection including preferences, MFA status, and granted permissions.
/// Used exclusively for the authenticated user's own profile (/me endpoint).
/// </summary>
public record MyProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsMfaEnabled,
    bool IsEmailVerified,
    string? PhoneNumber,
    string? AvatarUrl,
    string PreferredLanguage,
    string Timezone,
    List<string> Permissions,
    DateTime CreatedAt);

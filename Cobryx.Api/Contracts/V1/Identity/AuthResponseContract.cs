using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Result of an authentication or login attempt.
/// </summary>
public record AuthResponseContract(
    string? AccessToken,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? Email,
    string? Role,
    DateTime? Expires,
    Guid? SessionId = null,
    bool RequiresMfa = false,
    string? MfaToken = null,
    bool RequiresOnboarding = false
)
{
    /// <summary>The JWT access token for subsequent requests.</summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string? AccessToken { get; init; } = AccessToken;

    /// <summary>User's first name.</summary>
    /// <example>Jane</example>
    public string? FirstName { get; init; } = FirstName;

    /// <summary>User's last name.</summary>
    /// <example>Smith</example>
    public string? LastName { get; init; } = LastName;

    /// <summary>Combination of first and last name.</summary>
    /// <example>Jane Smith</example>
    public string? FullName { get; init; } = FullName;

    /// <summary>User's email address.</summary>
    /// <example>user@example.com</example>
    public string? Email { get; init; } = Email;

    /// <summary>User's primary role name.</summary>
    /// <example>Owner</example>
    public string? Role { get; init; } = Role;

    /// <summary>Expiration date/time of the access token (UTC).</summary>
    /// <example>2026-12-31T23:59:59Z</example>
    [Required]
    public DateTime? Expires { get; init; } = Expires;

    /// <summary>Unique identifier for the current session.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? SessionId { get; init; } = SessionId;

    /// <summary>True if the user must complete a second factor.</summary>
    /// <example>false</example>
    public bool RequiresMfa { get; init; } = RequiresMfa;

    /// <summary>Short-lived token required to complete MFA verification.</summary>
    /// <example>mfa_12345</example>
    public string? MfaToken { get; init; } = MfaToken;

    /// <summary>True if the business details are incomplete.</summary>
    /// <example>false</example>
    public bool RequiresOnboarding { get; init; } = RequiresOnboarding;
}

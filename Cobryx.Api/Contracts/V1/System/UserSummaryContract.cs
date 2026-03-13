namespace Cobryx.Api.Contracts.V1.System;

/// <summary>
/// Professional summary of a system user.
/// </summary>
public record UserSummaryContract(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive
)
{
    /// <summary>Unique identifier for the user.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>User's first name.</summary>
    /// <example>Jane</example>
    public string FirstName { get; init; } = FirstName;

    /// <summary>User's last name.</summary>
    /// <example>Smith</example>
    public string LastName { get; init; } = LastName;

    /// <summary>User's registered email address.</summary>
    /// <example>jane.smith@acme.mx</example>
    public string Email { get; init; } = Email;

    /// <summary>Primary security role name.</summary>
    /// <example>Owner</example>
    public string Role { get; init; } = Role;

    /// <summary>Current account status.</summary>
    /// <example>true</example>
    public bool IsActive { get; init; } = IsActive;
}

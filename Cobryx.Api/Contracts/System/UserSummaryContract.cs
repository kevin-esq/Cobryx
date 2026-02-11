using System;

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
);

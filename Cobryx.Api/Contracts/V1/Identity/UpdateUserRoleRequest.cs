using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Request payload for assigning a new role to a user.
/// </summary>
public record UpdateUserRoleRequest([Required] Guid RoleId)
{
    /// <summary>Unique identifier of the role to assign.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid RoleId { get; init; } = RoleId;
}

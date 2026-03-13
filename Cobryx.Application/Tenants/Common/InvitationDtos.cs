using Cobryx.Domain.Identity.Enums;


namespace Cobryx.Application.Tenants.Common;

public record InvitationDto(
    Guid Id,
    string Email,
    string RoleName,
    InvitationStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public record InviteUserRequest(
    string Email,
    string RoleName);

namespace Cobryx.Application.Users.Common;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string RoleName,
    DateTime CreatedAt);

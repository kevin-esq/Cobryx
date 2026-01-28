namespace Cobryx.Application.Users.Common;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string RoleName,
    DateTime CreatedAt);

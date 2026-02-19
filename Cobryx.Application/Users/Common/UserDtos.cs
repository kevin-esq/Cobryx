namespace Cobryx.Application.Users.Common;

public record UserListDto(
    Guid Id,
    string FullName,
    string Email,
    string RoleName,
    bool IsActive,
    DateTime CreatedAt);

public record RoleDto(
    Guid Id,
    string Name,
    string Description);

public record PagedList<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

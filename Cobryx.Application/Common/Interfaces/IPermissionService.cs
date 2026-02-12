namespace Cobryx.Application.Common.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken ct = default);
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default);
    Task InvalidateCacheAsync(Guid userId, CancellationToken ct = default);
}

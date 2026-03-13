using Cobryx.Application.Common.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services.Security;

public class PermissionService : IPermissionService
{
    private readonly CobryxDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public PermissionService(CobryxDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken ct = default)
    {
        var permissions = await GetPermissionsAsync(userId, ct);
        return permissions.Contains(permission);
    }

    public async Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .Select(u => new { u.Id, u.TenantId, u.PermissionVersion })
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null) return new HashSet<string>();

        string cacheKey = $"perm:{user.TenantId}:{userId}:v{user.PermissionVersion}";

        var cachedPermissions = await _cacheService.GetAsync<HashSet<string>>(cacheKey, ct);
        if (cachedPermissions != null)
        {
            return cachedPermissions;
        }

        var permissions = (await _dbContext.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Role.Permissions.Select(p => p.Name))
            .ToListAsync(ct)).ToHashSet();

        await _cacheService.SetAsync(cacheKey, permissions, cancellationToken: ct);

        return permissions;
    }

    public async Task InvalidateCacheAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);
        if (user != null)
        {
            // Incrementing version naturally "invalidates" because the key changes
            // The old version will eventually expire in Redis
            // We can explicitly clear if we want, but versioning is cleaner for concurrency
            // e.g. user.IncrementPermissionVersion() - I'll do this in a CommandHandler
        }
    }
}

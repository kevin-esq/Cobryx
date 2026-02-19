using Cobryx.Domain.Entities;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.Profile)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailLower = email.ToLowerInvariant();
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.Profile)
            .Include(u => u.Sessions)
            .Include(u => u.SecurityTokens)
            .FirstOrDefaultAsync(u => u.Email == (EmailAddress)emailLower, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailLower = email.ToLowerInvariant();
        return await _dbSet.AnyAsync(u => u.Email == (EmailAddress)emailLower, cancellationToken);
    }

    public async Task<User?> GetBySecurityTokenHashAsync(string tokenHash, Domain.Enums.SecurityTokenType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.SecurityTokens)
            .Include(u => u.Role)
            .Include(u => u.Profile)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.SecurityTokens.Any(t => t.TokenHash == tokenHash && t.Type == type), cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetByTenantPagedAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        var query = _dbSet
            .Include(u => u.Role)
            .Where(u => u.TenantId == tenantId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountAdminsInTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId && u.Role.Name == Role.Constants.Admin)
            .CountAsync(ct);
    }
}

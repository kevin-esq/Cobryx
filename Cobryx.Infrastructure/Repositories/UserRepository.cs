using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public override async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.RefreshTokens)
            .Include(u => u.Profile)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        var emailLower = email.ToLowerInvariant();
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.RefreshTokens)
            .Include(u => u.Profile)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Email == emailLower);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var emailLower = email.ToLowerInvariant();
        return await _dbSet.AnyAsync(u => u.Email == emailLower);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _dbSet
            .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.RefreshTokens)
            .Include(u => u.Profile)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
    }

    public async Task<User?> GetBySecurityTokenAsync(string token, Domain.Enums.SecurityTokenType type)
    {
        return await _dbSet
            .Include(u => u.SecurityTokens)
            .Include(u => u.Role)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.SecurityTokens.Any(t => t.Token == token && t.Type == type));
    }

    public async Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Where(u => u.TenantId == tenantId)
            .ToListAsync();
    }
}

using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var emailLower = email.ToLowerInvariant();
        return await _dbSet
            .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == emailLower);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var emailLower = email.ToLowerInvariant();
        return await _dbSet.AnyAsync(u => u.Email == emailLower);
    }
}

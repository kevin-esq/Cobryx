using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    public RoleRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == name);
    }
}

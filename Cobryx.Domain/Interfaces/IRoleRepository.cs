using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
}

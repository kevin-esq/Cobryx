using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

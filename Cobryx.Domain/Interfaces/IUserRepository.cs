using Cobryx.Domain.Identity;
using Cobryx.Domain.Identity.Enums;

namespace Cobryx.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    public Task<User?> GetBySecurityTokenHashAsync(string tokenHash, SecurityTokenType type, CancellationToken cancellationToken = default);
    public Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    public Task<(IEnumerable<User> Items, int TotalCount)> GetByTenantPagedAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    public Task<int> CountAdminsInTenantAsync(Guid tenantId, CancellationToken ct = default);
}

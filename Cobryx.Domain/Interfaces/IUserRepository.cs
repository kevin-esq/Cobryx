using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetBySecurityTokenHashAsync(string tokenHash, Enums.SecurityTokenType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Items, int TotalCount)> GetByTenantPagedAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAdminsInTenantAsync(Guid tenantId, CancellationToken ct = default);
}

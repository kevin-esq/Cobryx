using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetBySecurityTokenAsync(string token, Enums.SecurityTokenType type);
    Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId);
}

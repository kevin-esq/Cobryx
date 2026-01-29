using Cobryx.Domain.Entities;

namespace Cobryx.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, Role? roleOverride = null);
    string GenerateRefreshToken();
    string GenerateMfaToken(User user);
    Guid? ValidateMfaToken(string token);
}

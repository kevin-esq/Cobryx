using Cobryx.Domain.Identity;

namespace Cobryx.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    public string GenerateAccessToken(User user, Guid sessionId, Role? roleOverride = null);
    public string GenerateRefreshToken();
    public string GenerateMfaToken(User user);
    public Guid? ValidateMfaToken(string token);
}

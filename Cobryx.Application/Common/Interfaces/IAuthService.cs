using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Entities;

namespace Cobryx.Application.Common.Interfaces;

public interface IAuthService
{
    AuthResult GenerateAuthResponse(User user, Role? roleOverride = null);
    AuthResult RefreshAuthResponse(User user, string oldRefreshToken);
}

using System.Linq;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<AuthResult>>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IHttpContextService _httpContextService;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IAuthService authService,
        IHttpContextService httpContextService)
    {
        _userRepository = userRepository;
        _authService = authService;
        _httpContextService = httpContextService;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);

        if (user == null || !user.HasValidRefreshToken(request.RefreshToken))
        {
            return Result.Failure<AuthResult>("Invalid or active refresh token not found.");
        }

        var ipAddress = _httpContextService.GetIpAddress();
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var authResult = _authService.RefreshAuthResponse(user, request.RefreshToken, ipAddress, deviceFingerprint);

        await _userRepository.UpdateAsync(user);

        return Result.Success(authResult);
    }
}

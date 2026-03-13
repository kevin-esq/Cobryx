using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Exceptions.Auth;
using Cobryx.Domain.Exceptions.Users;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand() : IRequest<Result<AuthResult>>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthService _authService;
    private readonly IHttpContextService _httpContextService;
    private readonly ICookieService _cookieService;
    private readonly CobryxMetrics _metrics;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuthService authService,
        IHttpContextService httpContextService,
        ICookieService cookieService,
        CobryxMetrics metrics)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _authService = authService;
        _httpContextService = httpContextService;
        _cookieService = cookieService;
        _metrics = metrics;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenValue = _cookieService.GetRefreshTokenFromCookie();

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            throw new TokenMissingException();
        }

        var token = await _refreshTokenRepository.GetByTokenValueAsync(refreshTokenValue, cancellationToken);

        if (token == null)
        {
            throw new InvalidTokenException();
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);

        if (user == null)
        {
            throw new UserNotFoundException(token.UserId);
        }

        var ipAddress = _httpContextService.GetIpAddress();
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var authResult = await _authService.RefreshAuthResponse(user, token, ipAddress, deviceFingerprint, cancellationToken);

        _metrics.TokenRefreshes.Add(1);

        return Result.Success(authResult);
    }
}

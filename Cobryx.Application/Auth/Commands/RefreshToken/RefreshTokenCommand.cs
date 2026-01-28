using System.Linq;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<AuthResult>>
{
    public string IpAddress { get; init; } = "0.0.0.0";
    public string? DeviceFingerprint { get; init; }
}

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IAuthService authService)
    {
        _userRepository = userRepository;
        _authService = authService;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);

        if (user == null || !user.HasValidRefreshToken(request.RefreshToken))
        {
            return Result.Failure<AuthResult>("Invalid or active refresh token not found.");
        }

        var authResult = _authService.RefreshAuthResponse(user, request.RefreshToken, request.IpAddress, request.DeviceFingerprint);

        await _userRepository.UpdateAsync(user);

        return Result.Success(authResult);
    }
}

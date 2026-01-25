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
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. In a production app, we would validate the expired AccessToken to get the UserId
        // For now, we search for the user that owns this RefreshToken
        var users = await _userRepository.GetAllAsync(); // This is sub-optimal, but IUserRepository doesn't have SearchByToken yet
        var user = users.FirstOrDefault(u => u.HasValidRefreshToken(request.RefreshToken));

        if (user == null)
        {
            return Result.Failure<AuthResult>("Invalid or active refresh token not found.");
        }

        var activeToken = user.RefreshTokens.First(x => x.Token == request.RefreshToken);

        // 2. Rotate Token
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        activeToken.Revoke("unknown", newRefreshToken);
        user.AddRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7), "unknown");

        await _userRepository.UpdateAsync(user);

        return Result<AuthResult>.Success(new AuthResult(
            newAccessToken,
            newRefreshToken,
            user.Id,
            user.FullName,
            user.Role.Permissions.Select(p => p.Name)));
    }
}

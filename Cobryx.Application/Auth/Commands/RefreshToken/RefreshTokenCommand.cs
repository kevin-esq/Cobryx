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
        var users = await _userRepository.GetAllAsync(); 
        var user = users.FirstOrDefault(u => u.HasValidRefreshToken(request.RefreshToken));

        if (user == null)
        {
            return Result.Failure<AuthResult>("Invalid or active refresh token not found.");
        }

        var activeToken = user.RefreshTokens.First(x => x.Token == request.RefreshToken);

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        activeToken.Revoke("system-rotation", newRefreshToken);
        user.AddRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7), "token-rotation");

        await _userRepository.UpdateAsync(user);

        return Result.Success(new AuthResult(
            newAccessToken,
            newRefreshToken,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email,
            user.Role?.Name ?? "User",
            DateTime.UtcNow.AddMinutes(60)));
    }
}

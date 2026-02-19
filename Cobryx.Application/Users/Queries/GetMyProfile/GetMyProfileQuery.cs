using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Users.Queries.GetMyProfile;

public record MyProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsMfaEnabled,
    bool IsEmailVerified,
    string? PhoneNumber,
    string? AvatarUrl,
    string PreferredLanguage,
    string Timezone,
    List<string> Permissions,
    DateTime CreatedAt);

public record GetMyProfileQuery(Guid UserId) : IRequest<Result<MyProfileDto>>;

public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    private readonly IUserRepository _userRepository;

    public GetMyProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.Query()
            .AsNoTracking()
            .Include(u => u.Role)
                .ThenInclude(r => r!.Permissions)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) return Result.Failure<MyProfileDto>(DomainErrorCode.User.NotFound);

        return Result.Success(new MyProfileDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.Role?.Name ?? CobryxDefaults.UnknownValue,
            user.IsMfaEnabled,
            user.IsEmailVerified,
            user.Profile?.PhoneNumber,
            user.Profile?.AvatarUrl,
            user.Profile?.PreferredLanguage ?? CobryxDefaults.Locale,
            user.Profile?.Timezone ?? CobryxDefaults.Timezone,
            user.Role?.Permissions.Select(p => p.Name).ToList() ?? new List<string>(),
            user.CreatedAt));
    }
}

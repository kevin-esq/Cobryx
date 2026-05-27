using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Users.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Users.Queries.GetMyProfile
{
    /// <summary>
    /// Retrieves the full profile for the currently authenticated user.
    /// The user identity is resolved via ICurrentUserProvider — no userId parameter needed.
    /// </summary>
    [TenantScoped]
public record GetMyProfileQuery : IRequest<Result<MyProfileDto>>, IRequiresTenant;

    public class GetMyProfileHandler(
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider) : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
    {
        public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            Guid? userId = currentUserProvider.GetUserId();
            if (!userId.HasValue)
            {
                return Result.Failure<MyProfileDto>(DomainErrorCode.Auth.NotAuthenticated);
            }

            Domain.Identity.User? user = await userRepository.Query()
                .AsNoTracking()
                .Include(u => u.Role)
                .ThenInclude(r => r.Permissions)
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            return user == null
                ? Result.Failure<MyProfileDto>(DomainErrorCode.User.NotFound)
                : Result.Success(new MyProfileDto(
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email.Value,
                    user.Role.Name,
                    user.IsMfaEnabled,
                    user.IsEmailVerified,
                    user.Profile?.PhoneNumber,
                    user.Profile?.AvatarUrl,
                    user.Profile?.PreferredLanguage ?? CobryxDefaults.Locale,
                    user.Profile?.Timezone ?? CobryxDefaults.Timezone,
                    [.. user.Role.Permissions.Select(p => p.Name)],
                    user.CreatedAt));
        }
    }
}

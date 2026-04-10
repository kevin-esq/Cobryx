using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Users.Commands.UpdateMyProfile
{
    /// <summary>
    /// Updates the authenticated user's profile preferences.
    /// The user identity is resolved via ICurrentUserProvider — no userId parameter needed.
    /// </summary>
    public record UpdateMyProfileCommand(
        string? PhoneNumber,
        string? AvatarUrl,
        string PreferredLanguage,
        string Timezone) : IRequest<Result>;

    public class UpdateMyProfileHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserProvider currentUserProvider) : IRequestHandler<UpdateMyProfileCommand, Result>
    {
        public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            Guid? userId = currentUserProvider.GetUserId();
            if (!userId.HasValue)
            {
                return Result.Failure(DomainErrorCode.Auth.NotAuthenticated);
            }

            Domain.Identity.User? user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                return Result.Failure(DomainErrorCode.User.NotFound);
            }

            if (user.Profile == null)
            {
                user.CreateProfile(request.PhoneNumber, request.AvatarUrl);
            }

            user.Profile!.Update(request.PhoneNumber, request.AvatarUrl, request.PreferredLanguage, request.Timezone);

            await userRepository.UpdateAsync(user, cancellationToken);
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

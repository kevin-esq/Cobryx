using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Users.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(
    Guid UserId,
    string? PhoneNumber,
    string? AvatarUrl,
    string PreferredLanguage,
    string Timezone) : IRequest<Result>;

public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMyProfileHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return Result.Failure(DomainErrorCode.User.NotFound);

        if (user.Profile == null)
        {
            user.CreateProfile(request.PhoneNumber, request.AvatarUrl);
        }

        user.Profile!.Update(request.PhoneNumber, request.AvatarUrl, request.PreferredLanguage, request.Timezone);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

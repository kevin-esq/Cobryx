using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Auth.Commands.Sessions;

public record SessionResponse(Guid Id, string IpAddress, string? DeviceFingerprint, DateTime LastActiveAt, bool IsCurrent);

public record GetSessionsQuery : IRequest<Result<List<SessionResponse>>>;

public class GetSessionsHandler : IRequestHandler<GetSessionsQuery, Result<List<SessionResponse>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IHttpContextService _httpContextService;

    public GetSessionsHandler(
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider,
        IHttpContextService httpContextService)
    {
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
        _httpContextService = httpContextService;
    }

    public async Task<Result<List<SessionResponse>>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure<List<SessionResponse>>("User not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<List<SessionResponse>>("User not found.");

        // TODO: Include SessionId in claims to identify the "current" session reliably.

        var sessions = user.Sessions
            .Where(s => !s.IsRevoked)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new SessionResponse(s.Id, s.IpAddress, s.DeviceFingerprint, s.LastActiveAt, false))
            .ToList();

        return Result.Success(sessions);
    }
}

public record RevokeSessionCommand(Guid SessionId) : IRequest<Result<bool>>;

public class RevokeSessionHandler : IRequestHandler<RevokeSessionCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public RevokeSessionHandler(IUserRepository userRepository, ICurrentUserProvider currentUserProvider)
    {
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<bool>> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure<bool>("User not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<bool>("User not found.");

        user.RevokeSession(request.SessionId);
        await _userRepository.UpdateAsync(user);

        return Result.Success(true);
    }
}

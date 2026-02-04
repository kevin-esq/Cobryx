using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Auth.Commands.Sessions;

public record SessionResponse(Guid Id, string IpAddress, string? DeviceFingerprint, string? DeviceName, DateTime LastActiveAt, bool IsCurrent);

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

        var currentSessionId = _currentUserProvider.GetSessionId();

        var sessions = user.Sessions
            .Where(s => !s.IsRevoked)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new SessionResponse(s.Id, s.IpAddress, s.DeviceFingerprint, s.DeviceName, s.LastActiveAt, s.Id == currentSessionId))
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

public record LogoutCommand() : IRequest<Result>;

public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public LogoutHandler(IUserRepository userRepository, ICurrentUserProvider currentUserProvider)
    {
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        var sessionId = _currentUserProvider.GetSessionId();

        if (userId == null || sessionId == null) return Result.Failure("User not authenticated or session missing.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure("User not found.");

        user.RevokeSession(sessionId.Value);
        await _userRepository.UpdateAsync(user);

        return Result.Success();
    }
}

public record LogoutAllCommand() : IRequest<Result>;

public class LogoutAllHandler : IRequestHandler<LogoutAllCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public LogoutAllHandler(IUserRepository userRepository, ICurrentUserProvider currentUserProvider)
    {
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure("User not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure("User not found.");

        foreach (var session in user.Sessions.Where(s => !s.IsRevoked))
        {
            session.Revoke();
        }

        await _userRepository.UpdateAsync(user);

        return Result.Success();
    }
}

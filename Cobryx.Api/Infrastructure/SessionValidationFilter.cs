using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cobryx.Api.Infrastructure;

public class SessionValidationFilter : IAsyncActionFilter
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;

    public SessionValidationFilter(ICurrentUserProvider currentUserProvider, IUserRepository userRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = _currentUserProvider.GetUserId();
        var sessionId = _currentUserProvider.GetSessionId();

        if (userId.HasValue && sessionId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);
            
            if (user == null || !user.IsActive)
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "User account is disabled or missing." });
                return;
            }

            var session = user.Sessions.FirstOrDefault(s => s.Id == sessionId.Value);
            
            if (session == null || session.IsRevoked)
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Session has been revoked. Please log in again." });
                return;
            }
        }

        await next();
    }
}

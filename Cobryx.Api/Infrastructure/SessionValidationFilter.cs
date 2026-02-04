using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
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

            if (user == null || !user.IsActive || user.IsLocked)
            {
                context.Result = new UnauthorizedObjectResult(ApiResponse.FailureResponse("Account is locked, disabled or missing."));
                return;
            }

            var session = user.Sessions.FirstOrDefault(s => s.Id == sessionId.Value);

            if (session == null || session.IsRevoked)
            {
                context.Result = new UnauthorizedObjectResult(ApiResponse.FailureResponse("Session has been revoked. Please log in again."));
                return;
            }

            var skipCheck = context.ActionDescriptor.EndpointMetadata.Any(em => em is SkipOnboardingCheckAttribute);

            if (!skipCheck)
            {
                if (!user.IsEmailVerified)
                {
                    context.Result = new ObjectResult(ApiResponse.FailureResponse("Email verification is required.")) { StatusCode = 403 };
                    return;
                }

                if (user.RequiresOnboarding)
                {
                    context.Result = new ObjectResult(ApiResponse.FailureResponse("Business onboarding is required.")) { StatusCode = 403 };
                    return;
                }
            }
        }

        await next();
    }
}

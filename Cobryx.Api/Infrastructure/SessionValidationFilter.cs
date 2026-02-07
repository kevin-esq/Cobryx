using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Cobryx.Domain.Exceptions.Auth;
using Cobryx.Domain.Exceptions.Tenants;

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
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(em => em is AllowAnonymousAttribute);
        if (allowAnonymous)
        {
            await next();
            return;
        }

        var userId = _currentUserProvider.GetUserId();
        var sessionId = _currentUserProvider.GetSessionId();

        if (userId.HasValue && sessionId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);

            if (user == null || !user.IsActive || user.IsLocked)
            {
                throw new AccountLockedException();
            }

            var session = user.Sessions.FirstOrDefault(s => s.Id == sessionId.Value);

            if (session == null || session.IsRevoked)
            {
                throw new NotAuthenticatedException();
            }

            var skipCheck = context.ActionDescriptor.EndpointMetadata.Any(em => em is SkipOnboardingCheckAttribute);

            if (!skipCheck)
            {
                if (!user.IsEmailVerified)
                {
                    throw new EmailNotVerifiedException();
                }

                if (user.RequiresOnboarding)
                {
                    throw new OnboardingRequiredException();
                }
            }
        }

        await next();
    }
}

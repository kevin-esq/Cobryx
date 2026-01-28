using System.Security.Claims;
using Cobryx.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Cobryx.Infrastructure.MultiTenancy;

public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }

        return null;
    }
}

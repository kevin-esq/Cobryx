using Cobryx.Application.Common.Interfaces;

using Microsoft.AspNetCore.Http;

namespace Cobryx.Infrastructure.Services;

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string RefreshTokenCookieName = "refreshToken";

    public CookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void SetRefreshTokenCookie(string token, DateTime expires)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            IsEssential = true
        };

        context.Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
    }

    public string? GetRefreshTokenFromCookie()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request.Cookies[RefreshTokenCookieName];
    }

    public void DeleteRefreshTokenCookie()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return;

        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict
        });
    }
}

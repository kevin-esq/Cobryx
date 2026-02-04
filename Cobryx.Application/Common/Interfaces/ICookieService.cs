namespace Cobryx.Application.Common.Interfaces;

public interface ICookieService
{
    void SetRefreshTokenCookie(string token, DateTime expires);
    string? GetRefreshTokenFromCookie();
    void DeleteRefreshTokenCookie();
}

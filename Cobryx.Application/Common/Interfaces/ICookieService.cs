namespace Cobryx.Application.Common.Interfaces;

public interface ICookieService
{
    public void SetRefreshTokenCookie(string token, DateTime expires);
    public string? GetRefreshTokenFromCookie();
    public void DeleteRefreshTokenCookie();
}

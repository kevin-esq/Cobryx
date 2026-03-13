namespace Cobryx.Application.Common.Interfaces;

public interface IPasswordHasher
{
    public string HashPassword(string password);
    public bool VerifyPassword(string password, string passwordHash);
    public bool IsHashOutdated(string passwordHash);
}

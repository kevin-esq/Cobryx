namespace Cobryx.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    bool IsHashOutdated(string passwordHash);
}

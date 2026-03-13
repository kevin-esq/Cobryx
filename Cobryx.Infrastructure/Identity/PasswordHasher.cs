using System.Security.Cryptography;
using System.Text;

using Cobryx.Application.Common.Interfaces;

using Konscious.Security.Cryptography;

namespace Cobryx.Infrastructure.Identity;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10;
    private const int MemorySize = 65536;
    private const int DegreeOfParallelism = 4;

    public string HashPassword(string password)
    {
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        var hash = argon2.GetBytes(HashSize);

        return $"v1.{Iterations}.{MemorySize}.{DegreeOfParallelism}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 6 || parts[0] != "v1") return false;

            var iterations = int.Parse(parts[1]);
            var memorySize = int.Parse(parts[2]);
            var parallelism = int.Parse(parts[3]);
            var salt = Convert.FromBase64String(parts[4]);
            var hash = Convert.FromBase64String(parts[5]);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = parallelism,
                Iterations = iterations,
                MemorySize = memorySize
            };

            var newHash = argon2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, newHash);
        }
        catch
        {
            return false;
        }
    }

    public bool IsHashOutdated(string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 6 || parts[0] != "v1") return true;

            var iterations = int.Parse(parts[1]);
            return iterations < Iterations;
        }
        catch
        {
            return true;
        }
    }
}

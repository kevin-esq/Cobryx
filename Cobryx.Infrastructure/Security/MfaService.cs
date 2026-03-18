using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;

using OtpNet;

namespace Cobryx.Infrastructure.Security;

public class MfaService : IMfaService
{
    public string GenerateSecret()
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secretBytes);
    }

    public string GetQrCodeUri(User user, string secret)
    {
        var issuer = "Cobryx";
        var account = user.Email;
        return $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return false;

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public List<string> GenerateRecoveryCodes()
    {
        var codes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            codes.Add(Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper());
        }
        return codes;
    }
}

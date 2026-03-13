using Cobryx.Domain.Identity;

namespace Cobryx.Application.Common.Interfaces;

public interface IMfaService
{
    public string GenerateSecret();
    public string GetQrCodeUri(User user, string secret);
    public bool VerifyCode(string secret, string code);
    public List<string> GenerateRecoveryCodes();
}

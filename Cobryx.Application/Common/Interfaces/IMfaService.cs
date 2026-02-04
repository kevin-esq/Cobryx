using Cobryx.Domain.Entities;

namespace Cobryx.Application.Common.Interfaces;

public interface IMfaService
{
    string GenerateSecret();
    string GetQrCodeUri(User user, string secret);
    bool VerifyCode(string secret, string code);
    List<string> GenerateRecoveryCodes();
}

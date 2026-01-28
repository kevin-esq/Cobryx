namespace Cobryx.Application.Common.Interfaces;

public interface IHttpContextService
{
    string GetIpAddress();
    string GetUserAgent();
    string GetDeviceFingerprint();
}

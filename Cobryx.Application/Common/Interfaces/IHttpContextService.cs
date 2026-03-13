namespace Cobryx.Application.Common.Interfaces;

public interface IHttpContextService
{
    public string GetIpAddress();
    public string GetUserAgent();
    public string GetDeviceFingerprint();
}

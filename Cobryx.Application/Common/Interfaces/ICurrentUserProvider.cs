namespace Cobryx.Application.Common.Interfaces;

public interface ICurrentUserProvider
{
    public Guid? GetUserId();
    public Guid? GetSessionId();
}

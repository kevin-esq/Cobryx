namespace Cobryx.Application.Common.Interfaces;

public interface ICurrentUserProvider
{
    Guid? GetUserId();
}

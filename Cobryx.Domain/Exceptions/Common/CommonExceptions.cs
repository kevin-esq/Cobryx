using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Common;

public class EntityNotFoundException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Common.EntityNotFound;

    public EntityNotFoundException(string entityName, object key)
        : base()
    {
        Metadata.Add("Entity", entityName);
        Metadata.Add("Key", key);
    }
}

public class UnauthorizedContextException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Common.UnauthorizedContext;

    public UnauthorizedContextException()
        : base()
    {
    }
}

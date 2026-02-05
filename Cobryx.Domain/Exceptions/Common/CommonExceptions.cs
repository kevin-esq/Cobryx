using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Common;

public class EntityNotFoundException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Common.EntityNotFound;

    public EntityNotFoundException(string entityName, object key)
        : base()
    {
        Metadata.Add("Entity", entityName);
        Metadata.Add("Key", key);
    }
}

public class UnauthorizedContextException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Common.UnauthorizedContext;

    public UnauthorizedContextException()
        : base()
    {
    }
}

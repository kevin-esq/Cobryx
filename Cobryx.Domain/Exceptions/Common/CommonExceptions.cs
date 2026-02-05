using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Common;

public class EntityNotFoundException : CobryxException
{
    public override string ErrorCode => "DOMAIN.ENTITY_NOT_FOUND";

    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
        Metadata.Add("Entity", entityName);
        Metadata.Add("Key", key);
    }
}

public class UnauthorizedContextException : CobryxException
{
    public override string ErrorCode => "COMMON.UNAUTHORIZED_CONTEXT";

    public UnauthorizedContextException(string message = "Operation is not allowed in the current context.")
        : base(message)
    {
    }
}

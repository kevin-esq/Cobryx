namespace Cobryx.Domain.Shared;

public abstract class CobryxException : Exception
{
    public abstract DomainErrorCode ErrorCode { get; }
    public Dictionary<string, object> Metadata { get; }

    protected CobryxException(Dictionary<string, object>? metadata = null)
        : base()
    {
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

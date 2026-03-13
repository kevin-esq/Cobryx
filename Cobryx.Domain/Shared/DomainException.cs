namespace Cobryx.Domain.Shared;

public class DomainException : CobryxException
{
    private readonly DomainErrorCode _errorCode;
    public override DomainErrorCode ErrorCode => _errorCode;

    public DomainException(DomainErrorCode errorCode) : base()
    {
        _errorCode = errorCode;
    }
}

public class TooManyRequestsException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.System.TooManyRequests;

    public TooManyRequestsException() : base()
    {
    }
}

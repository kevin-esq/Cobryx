using System;

namespace Cobryx.Domain.Common;

public class DomainException : CobryxException
{
    private readonly string _errorCode;
    public override string ErrorCode => _errorCode;

    public DomainException(string errorCode) : base()
    {
        _errorCode = errorCode;
    }
}

public class TooManyRequestsException : CobryxException
{
    public override string ErrorCode => "SYSTEM.TOO_MANY_REQUESTS";

    public TooManyRequestsException() : base()
    {
    }
}

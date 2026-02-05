using System;

namespace Cobryx.Domain.Common;

public class DomainException : CobryxException
{
    public override string ErrorCode => "DOMAIN.GENERAL_ERROR";

    public DomainException(string message) : base(message)
    {
    }
}

public class TooManyRequestsException : CobryxException
{
    public override string ErrorCode => "SYSTEM.TOO_MANY_REQUESTS";

    public TooManyRequestsException(string message = "Too many requests. Please try again later.") : base(message)
    {
    }
}

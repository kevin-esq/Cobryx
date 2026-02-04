using System;

namespace Cobryx.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message = "Too many requests. Please try again later.") : base(message)
    {
    }
}

public class EmailUnverifiedException : Exception
{
    public EmailUnverifiedException(string message = "Email not verified") : base(message)
    {
    }
}

using System;

namespace Cobryx.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

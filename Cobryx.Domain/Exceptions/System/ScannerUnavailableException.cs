using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.System;

/// <summary>
/// Thrown when the virus scanner circuit breaker is open,
/// indicating ClamAV is temporarily unreachable.
/// </summary>
public class ScannerUnavailableException : DomainException
{
    public ScannerUnavailableException()
        : base(DomainErrorCode.Documents.ScannerUnavailable) { }
}

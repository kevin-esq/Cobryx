using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Exceptions.System;

public class SystemConfigurationException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Common.SystemConfigurationError;
    public SystemConfigurationException() : base() { }
}

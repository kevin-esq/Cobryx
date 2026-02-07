using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.System;

public class SystemConfigurationException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Common.SystemConfigurationError;
    public SystemConfigurationException() : base() { }
}

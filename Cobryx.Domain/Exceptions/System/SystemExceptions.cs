using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.System;

public class SystemConfigurationException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Common.SystemConfigurationError;
    public SystemConfigurationException() : base() { }
}

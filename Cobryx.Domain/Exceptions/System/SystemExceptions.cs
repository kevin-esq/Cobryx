using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.System;

public class SystemConfigurationException : CobryxException
{
    public override string ErrorCode => "SYSTEM.CONFIGURATION_ERROR";
    public SystemConfigurationException(string message) : base(message) { }
}

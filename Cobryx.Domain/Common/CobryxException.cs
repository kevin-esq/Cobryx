using System;
using System.Collections.Generic;

namespace Cobryx.Domain.Common;

public abstract class CobryxException : Exception
{
    public abstract string ErrorCode { get; }
    public Dictionary<string, object> Metadata { get; }

    protected CobryxException(Dictionary<string, object>? metadata = null)
        : base()
    {
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

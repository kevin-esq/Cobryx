using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Infrastructure.Services;

/// <summary>
/// Production implementation of IClock that uses standard DateTime.UtcNow.
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

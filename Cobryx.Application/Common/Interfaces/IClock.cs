namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Provides an abstraction for retrieving the current system time.
/// Essential for unit testing time-dependent logic.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime UtcNow { get; }
}

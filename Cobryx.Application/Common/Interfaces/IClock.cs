namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Provides an abstraction for retrieving the current system time.
/// Essential for unit testing time-dependent logic.
///
/// For deterministic replay, use ExecutionContext.FixedTime instead.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time in Coordinated Universal Time (UTC).
    /// In replay mode, returns the fixed time from ExecutionContext.
    /// </summary>
    public DateTime UtcNow { get; }

    /// <summary>
    /// Gets the actual system time, bypassing any replay/test overrides.
    /// Use sparingly - only for logging and metrics, never for business logic.
    /// </summary>
    public DateTime ActualUtcNow => DateTime.UtcNow;
}

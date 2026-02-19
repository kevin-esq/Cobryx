using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Security;

/// <summary>
/// Lightweight manual circuit breaker for the virus scanner.
/// Prevents scan request backlog when ClamAV is unreachable.
/// States: Closed (normal) → Open (failing) → HalfOpen (testing recovery).
/// Thread-safe via lock.
/// </summary>
public sealed class ScannerCircuitBreaker
{
    private readonly ILogger<ScannerCircuitBreaker> _logger;
    private readonly object _lock = new();

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTime _openedAtUtc;

    private const int FailureThreshold = 3;
    private static readonly TimeSpan OpenDuration = TimeSpan.FromSeconds(30);

    public ScannerCircuitBreaker(ILogger<ScannerCircuitBreaker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns true if the request should proceed. Returns false if the circuit is open.
    /// Transitions from Open → HalfOpen when the cool-down expires.
    /// </summary>
    public bool AllowRequest()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (DateTime.UtcNow - _openedAtUtc >= OpenDuration)
                    {
                        _state = CircuitState.HalfOpen;
                        _logger.LogInformation("Scanner circuit breaker → HalfOpen (testing recovery)");
                        return true;
                    }
                    return false;

                case CircuitState.HalfOpen:
                    return true;

                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// Records a successful scan. Resets failure count and closes the circuit.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation("Scanner circuit breaker → Closed (recovered)");
            }

            _state = CircuitState.Closed;
            _consecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Records a scan failure. Opens the circuit after reaching the failure threshold.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;

            if (_state == CircuitState.HalfOpen || _consecutiveFailures >= FailureThreshold)
            {
                _state = CircuitState.Open;
                _openedAtUtc = DateTime.UtcNow;
                _logger.LogWarning(
                    "Scanner circuit breaker → Open after {Failures} consecutive failures. Cool-down: {Duration}s",
                    _consecutiveFailures, OpenDuration.TotalSeconds);
            }
        }
    }

    /// <summary>Current state for diagnostics/metrics.</summary>
    public CircuitState CurrentState
    {
        get { lock (_lock) return _state; }
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}

namespace Cobryx.Application.Common.Execution;

/// <summary>
/// Deterministic execution context for time-travel debugging.
/// Captures all non-deterministic inputs at request start, allowing
/// exact replay of any historical request.
///
/// Usage:
/// - All time-dependent logic uses context.FixedTime instead of DateTime.UtcNow
/// - All random logic uses context.GetNextRandom() instead of Random
/// - External API responses are logged for replay
///
/// This enables:
/// - Exact reproduction of bugs
/// - Deterministic testing
/// - Audit trail for financial operations
/// </summary>
public class DeterministicContext
{
    /// <summary>
    /// Unique identifier for this execution.
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Fixed timestamp for this execution.
    /// All time-dependent logic should use this instead of DateTime.UtcNow.
    /// </summary>
    public DateTime FixedTime { get; }

    /// <summary>
    /// Tenant context for this execution.
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Random seed for deterministic random generation.
    /// </summary>
    public int RandomSeed { get; }

    private readonly Random _random;
    private readonly List<ExternalCallRecord> _externalCalls = new();
    private readonly object _lock = new();

    public DeterministicContext(
        Guid? executionId = null,
        string? correlationId = null,
        DateTime? fixedTime = null,
        Guid? tenantId = null,
        int? randomSeed = null)
    {
        ExecutionId = executionId ?? Guid.NewGuid();
        CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
        FixedTime = fixedTime ?? DateTime.UtcNow;
        TenantId = tenantId ?? Guid.Empty;
        RandomSeed = randomSeed ?? Environment.TickCount;
        _random = new Random(RandomSeed);
    }

    /// <summary>
    /// Get next deterministic random number.
    /// </summary>
    public int GetNextRandom()
    {
        lock (_lock)
        {
            return _random.Next();
        }
    }

    /// <summary>
    /// Get next deterministic random number in range.
    /// </summary>
    public int GetNextRandom(int minValue, int maxValue)
    {
        lock (_lock)
        {
            return _random.Next(minValue, maxValue);
        }
    }

    /// <summary>
    /// Get next deterministic random double.
    /// </summary>
    public double GetNextRandomDouble()
    {
        lock (_lock)
        {
            return _random.NextDouble();
        }
    }

    /// <summary>
    /// Record an external API call for replay.
    /// </summary>
    public void RecordExternalCall(string service, string operation, string? request, string? response)
    {
        lock (_lock)
        {
            _externalCalls.Add(new ExternalCallRecord(
                Sequence: _externalCalls.Count,
                Timestamp: DateTime.UtcNow,
                Service: service,
                Operation: operation,
                Request: request,
                Response: response));
        }
    }

    /// <summary>
    /// Get all recorded external calls for audit/replay.
    /// </summary>
    public IReadOnlyList<ExternalCallRecord> GetExternalCalls()
    {
        lock (_lock)
        {
            return _externalCalls.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Create a replay context from a previous execution.
    /// </summary>
    public static DeterministicContext ForReplay(
        Guid executionId,
        DateTime fixedTime,
        Guid tenantId,
        int randomSeed)
    {
        return new DeterministicContext(
            executionId: executionId,
            fixedTime: fixedTime,
            tenantId: tenantId,
            randomSeed: randomSeed);
    }
}

/// <summary>
/// Record of an external API call for audit and replay.
/// </summary>
public record ExternalCallRecord(
    int Sequence,
    DateTime Timestamp,
    string Service,
    string Operation,
    string? Request,
    string? Response);

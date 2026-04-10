namespace Cobryx.Application.Common.Execution;

/// <summary>
/// Provider for deterministic execution context.
/// Enables time-travel debugging by capturing all non-deterministic inputs.
/// </summary>
public interface IExecutionContextProvider
{
    /// <summary>
    /// Get the current deterministic context.
    /// </summary>
    public DeterministicContext Current { get; }

    /// <summary>
    /// Initialize a new deterministic context for the current request.
    /// </summary>
    public void Initialize(Guid? tenantId = null, string? correlationId = null);

    /// <summary>
    /// Initialize a replay context from historical data.
    /// </summary>
    public void InitializeForReplay(Guid executionId, DateTime fixedTime, Guid tenantId, int randomSeed);

    /// <summary>
    /// Clear the current deterministic context.
    /// </summary>
    public void Clear();
}

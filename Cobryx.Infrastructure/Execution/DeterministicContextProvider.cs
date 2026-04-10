using Cobryx.Application.Common.Execution;

namespace Cobryx.Infrastructure.Execution;

/// <summary>
/// AsyncLocal-based deterministic context provider.
/// Maintains deterministic context across async boundaries.
/// </summary>
public class DeterministicContextProvider : IExecutionContextProvider
{
    private static readonly AsyncLocal<DeterministicContext?> _current = new();

    public DeterministicContext Current =>
        _current.Value ?? throw new InvalidOperationException(
            "Deterministic context not initialized. Call Initialize() first.");

    public void Initialize(Guid? tenantId = null, string? correlationId = null)
    {
        _current.Value = new DeterministicContext(
            tenantId: tenantId,
            correlationId: correlationId);
    }

    public void InitializeForReplay(Guid executionId, DateTime fixedTime, Guid tenantId, int randomSeed)
    {
        _current.Value = DeterministicContext.ForReplay(executionId, fixedTime, tenantId, randomSeed);
    }

    public void Clear()
    {
        _current.Value = null;
    }
}

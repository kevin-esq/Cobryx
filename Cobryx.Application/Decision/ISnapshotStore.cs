using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision
{
    /// <summary>
    /// Asynchronously captures and persists decision snapshots for auditing and replaying.
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// Queues a decision for snapshotting. 
        /// Handles anonymization, hashing, and conditional full-state capture.
        /// </summary>
        public Task QueueSnapshotAsync(
            Guid customerId,
            string engineVersion,
            string configHash,
            object features,
            object macroState,
            object portfolioState,
            ExecutionTrace trace,
            DecisionResult result,
            bool isFull,
            CancellationToken ct = default);
    }
}

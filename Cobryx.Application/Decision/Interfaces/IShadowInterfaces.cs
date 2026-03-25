using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision.Interfaces;

public interface IShadowMonitor
{
    public Task RecordAsync(ShadowExecutionResult result, DecisionContext context);
}

public interface IShadowComparer
{
    public ShadowExecutionResult Compare(Guid snapshotId, ReplayResult primary, ReplayResult shadow, DriftToleranceProfile profile);
}

public interface IReplayEngine
{
    public Task<ReplayResult> ReplayAsync(IReplayInput input);
    public Task<List<ReplayResult>> ReplayBatchAsync(List<IReplayInput> inputs);
}

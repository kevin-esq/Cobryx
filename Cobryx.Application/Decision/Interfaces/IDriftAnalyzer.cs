using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;

namespace Cobryx.Application.Decision.Interfaces;

public interface IDriftAnalyzer
{
    public RegressionResult AnalyzeReplayResult(Guid snapshotId, ReplayResult replay, DriftToleranceProfile profile);
}

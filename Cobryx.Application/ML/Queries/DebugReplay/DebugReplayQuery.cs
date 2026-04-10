using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML.Queries.DebugReplay;

public record DebugReplayQuery(
    Guid SnapshotId,
    decimal LimitThreshold = 10,
    decimal RateThreshold = 0.01m) : IRequest<Result<DebugReplayResultDto>>;

public record DebugReplayResultDto(
    Guid SnapshotId,
    RegressionResult Drift,
    ReplayResult Replay);

/// <summary>
/// Orchestrates debug replay by delegating to ReplayEngine and DriftAnalyzer.
/// This handler is intentionally thin - all analysis logic lives in domain services.
/// </summary>
public class DebugReplayHandler(
    ICobryxDbContext db,
    ReplayEngine replayEngine,
    IDriftAnalyzer driftAnalyzer) : IRequestHandler<DebugReplayQuery, Result<DebugReplayResultDto>>
{
    public async Task<Result<DebugReplayResultDto>> Handle(DebugReplayQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch snapshot
        ProductionSnapshot? snapshot = await db.ProductionSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SnapshotId, cancellationToken);

        if (snapshot == null)
            return Result.Failure<DebugReplayResultDto>(DomainErrorCode.Common.EntityNotFound);

        // 2. Execute replay (delegated to ReplayEngine)
        var input = new ProductionSnapshotAdapter(snapshot);
        ReplayResult replayResult = await replayEngine.ReplayAsync(input);

        // 3. Analyze drift (delegated to DriftAnalyzer)
        var profile = new DriftToleranceProfile
        {
            CreditLimitAbsoluteThreshold = request.LimitThreshold,
            InterestRateRelativeThreshold = request.RateThreshold
        };
        RegressionResult drift = driftAnalyzer.AnalyzeReplayResult(snapshot.Id, replayResult, profile);

        // 4. Return result
        return Result.Success(new DebugReplayResultDto(request.SnapshotId, drift, replayResult));
    }
}

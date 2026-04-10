using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.ML.Commands.ExecuteReplay;

public record ExecuteReplayCommand(Guid SnapshotId) : IRequest<Result<ReplayResultDto>>;

public record ReplayResultDto(
    Guid SnapshotId,
    bool IsDeterministic,
    decimal DeltaCreditLimit,
    decimal DeltaInterestRate,
    bool TraceDriftDetected);

public partial class ExecuteReplayHandler(
    ReplayEngine replayEngine,
    ICobryxDbContext db,
    ILogger<ExecuteReplayHandler> logger) : IRequestHandler<ExecuteReplayCommand, Result<ReplayResultDto>>
{
    public async Task<Result<ReplayResultDto>> Handle(ExecuteReplayCommand request, CancellationToken cancellationToken)
    {
        LogReplayRequested(logger, request.SnapshotId);

        var snapshot = await db.ReplaySnapshots
            .FirstOrDefaultAsync(x => x.Id == request.SnapshotId, cancellationToken);

        if (snapshot == null)
        {
            LogSnapshotNotFound(logger, request.SnapshotId);
            return Result.Failure<ReplayResultDto>(DomainErrorCode.Common.EntityNotFound);
        }

        var adapter = new ReplaySnapshotAdapter(snapshot);
        ReplayResult result = await replayEngine.ReplayAsync(adapter);

        if (!result.IsDeterministic)
        {
            LogDeterminismDrift(logger, request.SnapshotId);
        }

        var dto = new ReplayResultDto(
            request.SnapshotId,
            result.IsDeterministic,
            result.DeltaCreditLimit,
            result.DeltaInterestRate,
            result.TraceDriftDetected);

        return Result.Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Replay requested for snapshot {SnapshotId}")]
    private static partial void LogReplayRequested(ILogger logger, Guid snapshotId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Snapshot {SnapshotId} not found")]
    private static partial void LogSnapshotNotFound(ILogger logger, Guid snapshotId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Determinism drift detected for snapshot {SnapshotId}")]
    private static partial void LogDeterminismDrift(ILogger logger, Guid snapshotId);
}

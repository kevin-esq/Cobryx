using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that handles stale documents:
/// - PendingScan > 15 min → re-enqueue scan
/// - PendingScan > 1 hour → mark ScanFailed
/// - Infected documents → verify blob deleted
/// </summary>
public class CleanupStaleDocumentsJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentStorage _storage;
    private readonly IClock _clock;
    private readonly ILogger<CleanupStaleDocumentsJob> _logger;

    private static readonly TimeSpan RetryThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AbandonThreshold = TimeSpan.FromHours(1);

    public CleanupStaleDocumentsJob(
        IUnitOfWork unitOfWork,
        IDocumentStorage storage,
        IClock clock,
        ILogger<CleanupStaleDocumentsJob> logger)
    {
        _unitOfWork = unitOfWork;
        _storage = storage;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var db = (DbContext)_unitOfWork;
        var now = _clock.UtcNow;

        var abandoned = await db.Set<DocumentMetadata>()
            .Where(d => d.ScanStatus == ScanStatus.PendingScan && d.CreatedAt < now - AbandonThreshold)
            .ToListAsync(ct);

        foreach (var doc in abandoned)
        {
            doc.MarkScanFailed("Scan timeout — exceeded 1 hour", now);
            _logger.LogWarning("Document {DocumentId} marked ScanFailed due to timeout", doc.Id);
        }

        var staleForRetry = await db.Set<DocumentMetadata>()
            .Where(d => d.ScanStatus == ScanStatus.ScanFailed
                     && d.ScannedAtUtc != null
                     && d.ScannedAtUtc < now - RetryThreshold
                     && d.CreatedAt > now - AbandonThreshold)
            .ToListAsync(ct);

        foreach (var doc in staleForRetry)
        {
            doc.ResetForRetry();
            _logger.LogInformation("Document {DocumentId} reset for scan retry", doc.Id);
        }

        var infected = await db.Set<DocumentMetadata>()
            .Where(d => d.ScanStatus == ScanStatus.Infected)
            .ToListAsync(ct);

        foreach (var doc in infected)
        {
            try
            {
                await _storage.DeleteAsync(doc.BlobPath);
            }
            catch
            {
            }
        }

        if (abandoned.Count > 0 || staleForRetry.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Cleanup: {Abandoned} abandoned, {Retried} reset for retry, {Infected} infected verified",
                abandoned.Count, staleForRetry.Count, infected.Count);
        }
    }
}

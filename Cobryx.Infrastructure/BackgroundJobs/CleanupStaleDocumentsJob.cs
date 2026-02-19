using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
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

        // 1. Abandoned: PendingScan > 1 hour → mark ScanFailed
        var abandoned = await db.Set<DocumentMetadata>()
            .Where(d => d.ScanStatus == ScanStatus.PendingScan && d.CreatedAt < now - AbandonThreshold)
            .ToListAsync(ct);

        foreach (var doc in abandoned)
        {
            doc.MarkScanFailed("Scan timeout — exceeded 1 hour", now);
            _logger.LogWarning("Document {DocumentId} marked ScanFailed due to timeout", doc.Id);
        }

        // 2. Stale: ScanFailed → reset for retry (one attempt)
        var staleForRetry = await db.Set<DocumentMetadata>()
            .Where(d => d.ScanStatus == ScanStatus.ScanFailed
                     && d.ScannedAtUtc != null
                     && d.ScannedAtUtc < now - RetryThreshold
                     && d.CreatedAt > now - AbandonThreshold) // Only retry recent ones
            .ToListAsync(ct);

        foreach (var doc in staleForRetry)
        {
            doc.ResetForRetry();
            _logger.LogInformation("Document {DocumentId} reset for scan retry", doc.Id);
        }

        // 3. Infected cleanup: verify blob actually deleted
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
                // Already deleted or storage unreachable — acceptable
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

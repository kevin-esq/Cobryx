using System.Diagnostics;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Documents.Commands.ScanDocument;

/// <summary>
/// Background handler invoked by Outbox dispatcher after DocumentUploadedEvent.
/// Downloads the file from storage and scans it for viruses.
/// Idempotent: only scans documents still in PendingScan state.
/// Max retry: 3 attempts before marking as ScanFailed.
/// </summary>
public class ScanDocumentHandler(
    IUnitOfWork unitOfWork,
    IDocumentStorage storage,
    IVirusScanner scanner,
    IClock clock,
    CobryxMetrics metrics,
    ILogger<ScanDocumentHandler> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentStorage _storage = storage;
    private readonly IVirusScanner _scanner = scanner;
    private readonly IClock _clock = clock;
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromMinutes(15);
    private readonly CobryxMetrics _metrics = metrics;
    private readonly ILogger<ScanDocumentHandler> _logger = logger;
    private const int MaxRetryAttempts = 3;

    public async Task ExecuteAsync(Guid documentId, int attempt = 1, CancellationToken ct = default)
    {
        var db = (DbContext)_unitOfWork;
        var document = await db.Set<DocumentMetadata>()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null)
        {
            _logger.LogWarning("ScanDocument: Document {DocumentId} not found", documentId);
            return;
        }

        // Idempotency guard — only scan PendingScan documents
        if (document.ScanStatus != ScanStatus.PendingScan)
        {
            _logger.LogInformation("ScanDocument: Document {DocumentId} already in state {Status}, skipping",
                documentId, document.ScanStatus);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var fileStream = await _storage.DownloadAsync(document.BlobPath);
            if (fileStream == null)
            {
                document.MarkScanFailed("File not found in storage", _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(ct);
                _metrics.DocumentsScanFailTotal.Add(1);
                return;
            }

            using (fileStream)
            {
                var isSafe = await _scanner.IsSafeAsync(fileStream);
                stopwatch.Stop();

                _metrics.DocumentsScanTotal.Add(1);
                _metrics.DocumentsScanLatency.Record(stopwatch.Elapsed.TotalSeconds);

                if (isSafe)
                {
                    document.MarkAsClean(_clock.UtcNow);
                    _logger.LogInformation("Document {DocumentId} scanned clean", documentId);
                }
                else
                {
                    document.MarkAsInfected(_clock.UtcNow);
                    _metrics.DocumentsInfectedTotal.Add(1);

                    try
                    {
                        await _storage.DeleteAsync(document.BlobPath);
                        _logger.LogWarning("Infected document {DocumentId} deleted from storage", documentId);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to delete infected file {BlobPath}", document.BlobPath);
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.DocumentsScanFailTotal.Add(1);

            var reason = ex switch
            {
                TimeoutException => "Scanner timeout",
                System.Net.Sockets.SocketException => "Scanner unreachable",
                _ => $"Scan error: {ex.Message}"
            };

            if (attempt >= MaxRetryAttempts)
            {
                document.MarkScanFailed(reason, _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogError(ex, "ScanDocument: Failed after {Attempts} attempts for {DocumentId}. Reason: {Reason}",
                    attempt, documentId, reason);
            }
            else
            {
                _logger.LogWarning(ex, "ScanDocument: Attempt {Attempt}/{Max} failed for {DocumentId}. Will retry.",
                    attempt, MaxRetryAttempts, documentId);
                throw;
            }
        }
    }
}

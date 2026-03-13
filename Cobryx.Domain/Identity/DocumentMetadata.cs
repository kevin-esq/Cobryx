using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;


namespace Cobryx.Domain.Identity;

public class DocumentMetadata : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid EntityId { get; private set; }
    public string EntityType { get; private set; }
    public string FileName { get; private set; }
    public string BlobPath { get; private set; }
    public long FileSize { get; private set; }
    public string MimeType { get; private set; }
    public Guid UploadedBy { get; private set; }
    public ScanStatus ScanStatus { get; private set; }
    public DateTime? ScannedAtUtc { get; private set; }
    public string? ScanFailureReason { get; private set; }

    private DocumentMetadata()
    {
        EntityType = null!;
        FileName = null!;
        BlobPath = null!;
        MimeType = null!;
    }

    public DocumentMetadata(
        Guid tenantId,
        Guid entityId,
        string entityType,
        string fileName,
        string blobPath,
        long fileSize,
        string mimeType,
        Guid uploadedBy,
        DateTime createdAtUtc)
    {
        TenantId = tenantId;
        EntityId = entityId;
        EntityType = entityType;
        FileName = fileName;
        BlobPath = blobPath;
        FileSize = fileSize;
        MimeType = mimeType;
        UploadedBy = uploadedBy;
        ScanStatus = ScanStatus.PendingScan;

        AddDomainEvent(new Events.Documents.DocumentUploadedEvent(
            Id, tenantId, fileName, blobPath, createdAtUtc));
    }

    public void MarkAsClean(DateTime scannedAt)
    {
        ScanStatus = ScanStatus.Clean;
        ScannedAtUtc = scannedAt;
        ScanFailureReason = null;
    }

    public void MarkAsInfected(DateTime scannedAt)
    {
        ScanStatus = ScanStatus.Infected;
        ScannedAtUtc = scannedAt;
        ScanFailureReason = null;
    }

    public void MarkScanFailed(string reason, DateTime scannedAt)
    {
        ScanStatus = ScanStatus.ScanFailed;
        ScannedAtUtc = scannedAt;
        ScanFailureReason = reason;
    }

    /// <summary>
    /// Resets to PendingScan for retry. Only valid from ScanFailed state.
    /// </summary>
    public void ResetForRetry()
    {
        if (ScanStatus != ScanStatus.ScanFailed) return;
        ScanStatus = ScanStatus.PendingScan;
        ScanFailureReason = null;
    }
}

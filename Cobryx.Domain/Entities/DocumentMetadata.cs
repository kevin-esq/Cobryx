using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

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
        Guid uploadedBy)
    {
        TenantId = tenantId;
        EntityId = entityId;
        EntityType = entityType;
        FileName = fileName;
        BlobPath = blobPath;
        FileSize = fileSize;
        MimeType = mimeType;
        UploadedBy = uploadedBy;
    }
}

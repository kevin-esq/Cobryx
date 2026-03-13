using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Services;

public class DocumentService
{
    private readonly IDocumentStorage _storage;
    private readonly IVirusScanner _scanner;
    private const long MaxFileSize = 10 * 1024 * 1024;

    public DocumentService(IDocumentStorage storage, IVirusScanner scanner)
    {
        _storage = storage;
        _scanner = scanner;
    }

    public async Task<DocumentMetadata> ProcessUploadAsync(
        Guid tenantId,
        Guid entityId,
        string entityType,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid uploadedBy,
        DateTime createdAtUtc)
    {
        if (fileStream.Length > MaxFileSize)
        {
            throw new DomainException(DomainErrorCode.Documents.FileSizeExceeded);
        }

        if (!await _scanner.IsSafeAsync(fileStream))
        {
            throw new DomainException(DomainErrorCode.Documents.VirusDetected);
        }

        fileStream.Position = 0;
        var blobPath = await _storage.UploadAsync(fileStream, fileName, contentType);

        return new DocumentMetadata(
            tenantId,
            entityId,
            entityType,
            fileName,
            blobPath,
            fileStream.Length,
            contentType,
            uploadedBy,
            createdAtUtc);
    }
}

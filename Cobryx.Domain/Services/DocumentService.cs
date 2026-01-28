using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

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
        Guid uploadedBy)
    {
        if (fileStream.Length > MaxFileSize)
        {
            throw new InvalidOperationException("File size exceeds the 10MB limit.");
        }

        if (!await _scanner.IsSafeAsync(fileStream))
        {
            throw new InvalidOperationException("File contains a virus and cannot be uploaded.");
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
            uploadedBy);
    }
}

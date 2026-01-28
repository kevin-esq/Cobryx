namespace Cobryx.Domain.Interfaces;

public interface IDocumentStorage
{
    Task<string> UploadAsync(Stream file, string fileName, string contentType);
    Task<Stream?> DownloadAsync(string blobPath);
    Task DeleteAsync(string blobPath);
    Task<string> GetPreSignedUrlAsync(string blobPath, TimeSpan expiry);
}

namespace Cobryx.Domain.Interfaces;

public interface IDocumentStorage
{
    public Task<string> UploadAsync(Stream file, string fileName, string contentType);
    public Task<Stream?> DownloadAsync(string blobPath);
    public Task DeleteAsync(string blobPath);
    public Task<string> GetPreSignedUrlAsync(string blobPath, TimeSpan expiry);
}

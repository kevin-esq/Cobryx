using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Cobryx.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.FileStorage;

public class AzureStorageProvider : IDocumentStorage
{
    private readonly BlobServiceClient _serviceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureStorageProvider> _logger;

    public AzureStorageProvider(IConfiguration configuration, ILogger<AzureStorageProvider> logger)
    {
        var connectionString = configuration["Storage:AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage ConnectionString is missing.");
        _containerName = configuration["Storage:AzureBlob:ContainerName"] ?? "documents";
        _serviceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream file, string fileName, string contentType)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobPath = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        await blobClient.UploadAsync(file, new BlobHttpHeaders { ContentType = contentType });

        _logger.LogInformation("Uploaded file {FileName} to Azure Blob Storage at {BlobPath}", fileName, blobPath);
        return blobPath;
    }

    public async Task<Stream?> DownloadAsync(string blobPath)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync()) return null;

        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobPath)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<string> GetPreSignedUrlAsync(string blobPath, TimeSpan expiry)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("BlobClient cannot generate SAS URI. Check connection string permissions.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return await Task.FromResult(sasUri.ToString());
    }
}

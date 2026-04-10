using Amazon.S3;
using Amazon.S3.Model;

using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Infrastructure.Services.FileStorage;

public class R2StorageProvider : IDocumentStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<R2StorageProvider> _logger;

    public R2StorageProvider(IOptions<S3StorageOptions> options, ILogger<R2StorageProvider> logger)
    {
        var s3Options = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = s3Options.ServiceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
        _bucketName = s3Options.BucketName;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream file, string fileName, string contentType)
    {
        var blobPath = $"{Guid.NewGuid()}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = blobPath,
            InputStream = file,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request);

        _logger.LogInformation("Uploaded file {FileName} to Cloudflare R2 at {BlobPath}", fileName, blobPath);
        return blobPath;
    }

    public async Task<Stream?> DownloadAsync(string blobPath)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = blobPath
            };

            var response = await _s3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string blobPath)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = blobPath
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    public async Task<string> GetPreSignedUrlAsync(string blobPath, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = blobPath,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }
}

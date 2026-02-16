using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Cobryx.Infrastructure.Configuration;
using nClam;

namespace Cobryx.Infrastructure.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCobryxHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var s3AccessKey = configuration["Storage:S3:AccessKey"]
            ?? throw new InvalidOperationException("S3 AccessKey is missing.");
        var s3SecretKey = configuration["Storage:S3:SecretKey"]
            ?? throw new InvalidOperationException("S3 SecretKey is missing.");
        var s3ServiceUrl = configuration["Storage:S3:ServiceUrl"]
            ?? throw new InvalidOperationException("S3 ServiceUrl is missing.");
        var bucketName = configuration["Storage:S3:BucketName"]
            ?? throw new InvalidOperationException("S3 BucketName is missing.");

        var clamAvSection = configuration.GetSection(ClamAvOptions.SectionName);
        var clamAvHost = clamAvSection["Host"] ?? throw new InvalidOperationException("ClamAV Host is missing.");
        var clamAvPort = int.TryParse(clamAvSection["Port"], out var port) ? port : 3310;

        var redisConnectionString = configuration["Caching:Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis ConnectionString is missing.");

        services.AddHealthChecks()
            .AddNpgSql(dbConnectionString, name: "Database")
            .AddS3(options =>
            {
                options.BucketName = bucketName;
                options.Credentials = new Amazon.Runtime.BasicAWSCredentials(s3AccessKey, s3SecretKey);
                options.S3Config = new Amazon.S3.AmazonS3Config
                {
                    ServiceURL = s3ServiceUrl,
                    ForcePathStyle = true
                };
            }, name: "CloudflareR2")
            .AddAsyncCheck("ClamAV", async () =>
            {
                try
                {
                    var clam = new ClamClient(clamAvHost, clamAvPort);
                    var ping = await clam.PingAsync();
                    return ping ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("ClamAV Ping failed.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"ClamAV Unreachable: {ex.Message}");
                }
            })
            .AddCheck("Redis", new RedisHealthCheck(redisConnectionString))
            .AddCheck<OutboxHealthCheck>("Outbox");

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
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
        var bucketName = configuration["Storage:S3:BucketName"] ?? "documents";

        var clamAvHost = configuration["Security:ClamAV:Host"] ?? "localhost";
        var clamAvPort = int.Parse(configuration["Security:ClamAV:Port"] ?? "3310");

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
            .AddCheck("Redis", new RedisHealthCheck(configuration["Caching:Redis:ConnectionString"] ?? "localhost:6379"))
            .AddCheck<OutboxHealthCheck>("Outbox");

        return services;
    }
}

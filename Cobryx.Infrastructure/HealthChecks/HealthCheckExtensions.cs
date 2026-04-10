using Cobryx.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using nClam;

namespace Cobryx.Infrastructure.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCobryxHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddHealthChecks()
            .AddNpgSql(dbConnectionString, name: "Database")
            .AddS3(options =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var s3Options = scope.ServiceProvider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

                options.BucketName = s3Options.BucketName;
                options.Credentials = new Amazon.Runtime.BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);
                options.S3Config = new Amazon.S3.AmazonS3Config
                {
                    ServiceURL = s3Options.ServiceUrl,
                    ForcePathStyle = true
                };
            }, name: "CloudflareR2")
            .AddAsyncCheck("ClamAV", async () =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var clamOptions = scope.ServiceProvider.GetRequiredService<IOptions<ClamAvOptions>>().Value;

                try
                {
                    var clam = new ClamClient(clamOptions.Host, clamOptions.Port);
                    var ping = await clam.PingAsync();
                    return ping
                        ? HealthCheckResult.Healthy()
                        : HealthCheckResult.Unhealthy("ClamAV ping failed.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"ClamAV unreachable: {ex.Message}");
                }
            })
            .AddCheck("Redis", new RedisHealthCheck(configuration))
            .AddCheck<OutboxHealthCheck>("Outbox")
            .AddCheck<ShadowHealthCheck>("ShadowDrift");

        return services;
    }
}

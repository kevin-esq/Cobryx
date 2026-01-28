using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using nClam;
using Azure.Storage.Blobs;

namespace Cobryx.Infrastructure.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCobryxHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var azureConnectionString = configuration["Storage:AzureBlob:ConnectionString"] 
            ?? throw new InvalidOperationException("Azure Storage ConnectionString is missing.");
        
        var containerName = configuration["Storage:AzureBlob:ContainerName"] ?? "documents";

        var clamAvHost = configuration["Security:ClamAV:Host"] ?? "localhost";
        var clamAvPort = int.Parse(configuration["Security:ClamAV:Port"] ?? "3310");

        services.AddHealthChecks()
            .AddNpgSql(dbConnectionString, name: "Database")
            .AddAzureBlobStorage(
                azureConnectionString, 
                containerName: containerName, 
                name: "AzureBlobStorage")
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
            .AddRedis(
                configuration["Caching:Redis:ConnectionString"] ?? "localhost:6379",
                name: "Redis");

        return services;
    }
}

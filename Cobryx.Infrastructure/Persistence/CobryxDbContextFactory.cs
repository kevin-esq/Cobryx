using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using Moq;

namespace Cobryx.Infrastructure.Persistence;

public class CobryxDbContextFactory : IDesignTimeDbContextFactory<CobryxDbContext>
{
    public CobryxDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var apiPath = Path.Combine(basePath, "..", "Cobryx.Api");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiPath) ? apiPath : basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<CobryxDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=cobryx_dev;Username=postgres;Password=postgres";

        builder.UseNpgsql(connectionString);

        return new CobryxDbContext(builder.Options, new Mock<ITenantProvider>().Object);
    }
}

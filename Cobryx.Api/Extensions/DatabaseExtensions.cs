using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace Cobryx.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await MigrateDatabaseAsync(app, dbContext);
        await SeedDatabaseAsync(dbContext, roleRepo, unitOfWork);
    }

    private static async Task MigrateDatabaseAsync(WebApplication app, CobryxDbContext dbContext)
    {
        var shouldMigrate = app.Environment.IsDevelopment()
            || app.Environment.IsEnvironment("Testing")
            || Environment.GetEnvironmentVariable("ENABLE_MIGRATION") == "true";

        if (!shouldMigrate)
            return;

        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await dbContext.Database.EnsureCreatedAsync();
            Log.Information("SQLite database created from model (skipping PG-specific migrations)");
        }
        else
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migration completed successfully");
        }
    }

    private static async Task SeedDatabaseAsync(
        CobryxDbContext dbContext,
        IRoleRepository roleRepo,
        IUnitOfWork unitOfWork)
    {
        await DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
        await DbInitializer.SeedPlansAsync(dbContext);
        await DbInitializer.SeedPlatformTenantAsync(dbContext);
        Log.Information("Database seeding completed successfully");
    }
}

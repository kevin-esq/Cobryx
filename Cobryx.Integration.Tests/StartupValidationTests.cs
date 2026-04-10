using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cobryx.Integration.Tests;

public class StartupValidationTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
{
    [Fact]
    public void Startup_ShouldFail_WhenRequiredConfigurationIsMissing()
    {
        WebApplicationFactory<Program> builder = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:Secret", "" },
                    { "Stripe:SecretKey", "" },
                    { "Email:FromAddress", "" }
                });
            });
        });

        Action act = () => { _ = builder.CreateClient(); };

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void DbContext_Should_Register_All_EfCore_Interceptors()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        CobryxDbContext context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        _ = context;

        AuditInterceptor? auditInterceptor = scope.ServiceProvider.GetService<AuditInterceptor>();
        OutboxInterceptor? outboxInterceptor = scope.ServiceProvider.GetService<OutboxInterceptor>();
        DbMetricsInterceptor? metricsInterceptor = scope.ServiceProvider.GetService<DbMetricsInterceptor>();
        EntityUpdateInterceptor? updateInterceptor = scope.ServiceProvider.GetService<EntityUpdateInterceptor>();

        auditInterceptor.Should().NotBeNull();
        outboxInterceptor.Should().NotBeNull();
        metricsInterceptor.Should().NotBeNull();
        updateInterceptor.Should().NotBeNull();
    }
}

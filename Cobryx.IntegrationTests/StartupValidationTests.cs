using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cobryx.IntegrationTests;

public class StartupValidationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public void Startup_ShouldFail_WhenRequiredConfigurationIsMissing()
    {
        var builder = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:Secret", "" },
                    { "Stripe:SecretKey", "" },
                    { "Email:FromAddress", "" }
                });
            });
        });

        Action act = () => { var client = builder.CreateClient(); };

        act.Should().Throw<OptionsValidationException>()
           .WithMessage("*DataAnnotation validation failed for 'JwtOptions'*");
    }

    [Fact]
    public void DbContext_Should_Register_All_EfCore_Interceptors()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();

        var auditInterceptor = scope.ServiceProvider.GetService<AuditInterceptor>();
        var outboxInterceptor = scope.ServiceProvider.GetService<OutboxInterceptor>();
        var metricsInterceptor = scope.ServiceProvider.GetService<DbMetricsInterceptor>();

        auditInterceptor.Should().NotBeNull();
        outboxInterceptor.Should().NotBeNull();
        metricsInterceptor.Should().NotBeNull();
    }
}

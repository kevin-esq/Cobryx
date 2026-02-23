using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cobryx.IntegrationTests;

public class StartupValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StartupValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Startup_ShouldFail_WhenRequiredConfigurationIsMissing()
    {
        // Arrange
        var builder = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Wipe out production/test config and provide empty settings
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:Secret", "" }, // Required
                    { "Stripe:SecretKey", "" }, // Required
                    { "Email:FromAddress", "" } // Required (FromAddress is required)
                });
            });
        });

        // Act & Assert
        // ValidateOnStart causes the exception during host build (Server creation)
        Action act = () => { var client = builder.CreateClient(); };

        act.Should().Throw<OptionsValidationException>()
           .WithMessage("*DataAnnotation validation failed for 'JwtOptions'*");
    }
}

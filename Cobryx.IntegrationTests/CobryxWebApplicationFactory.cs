using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Application.Common.Interfaces;
using Cobryx.IntegrationTests.Fakes;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Cobryx.IntegrationTests;

public class CobryxWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:Secret", "SuperSecretKeyForIntegrationTests1234567890!" },
                { "JwtSettings:Issuer", "CobryxApi-Test" },
                { "JwtSettings:Audience", "CobryxClient-Test" },
                { "JwtSettings:ExpiryMinutes", "60" }
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var secret = "SuperSecretKeyForIntegrationTests1234567890!";
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                options.TokenValidationParameters.ValidIssuer = "CobryxApi-Test";
                options.TokenValidationParameters.ValidAudience = "CobryxClient-Test";
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            });

            services.Configure<Microsoft.AspNetCore.Builder.CookiePolicyOptions>(options =>
            {
                options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            });

            var backgroundServices = services.Where(d => d.ServiceType.Name.Contains("IHostedService") || d.ImplementationType?.Name.Contains("Job") == true).ToList();
            foreach (var service in backgroundServices)
            {
                services.Remove(service);
            }

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CobryxDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);

            services.AddSingleton<IEmailService, MockEmailService>();
            services.AddSingleton<MockEmailService>(sp => (MockEmailService)sp.GetRequiredService<IEmailService>());

            var captchaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICaptchaService));
            if (captchaDescriptor != null) services.Remove(captchaDescriptor);

            services.AddSingleton<ICaptchaService, MockCaptchaService>();

            var attemptDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthAttemptService));
            if (attemptDescriptor != null) services.Remove(attemptDescriptor);

            services.AddSingleton<IAuthAttemptService, MockAuthAttemptService>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CobryxDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContext<CobryxDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}

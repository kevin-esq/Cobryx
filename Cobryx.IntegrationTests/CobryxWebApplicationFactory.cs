using System.Data.Common;
using System.Text;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;
using Cobryx.IntegrationTests.Fakes;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Cobryx.IntegrationTests;

public class CobryxWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", "Testing" },
                { "Caching:UseInMemory", "true" },
                { "JwtSettings:Secret", "SuperSecretKeyForTesting!LengthMustBeAtLeast32Chars" },
                { "JwtSettings:Issuer", "CobryxTest" },
                { "JwtSettings:Audience", "CobryxTest" },
                { "JwtSettings:ExpiryMinutes", "60" },
                { "App:BaseUrl", "https://test.cobryx.com" },
                { "App:AppUrl", "https://app-test.cobryx.com" },
                { "App:DocsUrl", "https://docs-test.cobryx.com" },
                { "Email:SmtpHost", "localhost" },
                { "Email:SmtpPort", "25" },
                { "Email:FromAddress", "noreply@test.cobryx.com" },
                { "Stripe:SecretKey", "sk_test_123" },
                { "Stripe:WebhookSecret", "whsec_test_123" },
                { "Stripe:SuccessUrl", "https://test.cobryx.com/success" },
                { "Stripe:CancelUrl", "https://test.cobryx.com/cancel" },
                { "App:InvitationTokenSecret", "TestSecretKey1234567890!Length32Chars" },
                { "Security:Captcha:SecretKey", "6LeIxAcTAAAAAGG-vFI1TnRWxMZ_DM7Oru8nEPvM" },
                { "Stripe:PaymentLinkSecret", "test_pl_secret_12345678901234567890" },
                { "Hangfire:UseMemoryStorage", "true" },
                { "Storage:S3:AccessKey", "test-access-key" },
                { "Storage:S3:SecretKey", "test-secret-key" },
                { "Storage:S3:ServiceUrl", "https://localhost:9000" },
                { "Storage:S3:BucketName", "documents-test" }
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var secret = "SuperSecretKeyForTesting!LengthMustBeAtLeast32Chars";
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                options.TokenValidationParameters.ValidIssuer = "CobryxTest";
                options.TokenValidationParameters.ValidAudience = "CobryxTest";
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

            services.AddSingleton<IAuthAttemptService, MockAuthAttemptService>();

            var stripeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStripeService));
            if (stripeDescriptor != null) services.Remove(stripeDescriptor);

            services.AddSingleton<IStripeService, MockStripeService>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CobryxDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContext<CobryxDbContext>((sp, options) =>
            {
                options.AddInterceptors(
                sp.GetRequiredService<OutboxInterceptor>(),
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<AuditFieldsInterceptor>(),
                sp.GetRequiredService<DbMetricsInterceptor>());
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });


            var tenantDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITenantProvider));
            if (tenantDescriptor != null) services.Remove(tenantDescriptor);
            services.AddSingleton<ITenantProvider, TestTenantProvider>();

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    public class TestTenantProvider : ITenantProvider
    {
        public Guid TenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public Guid? GetTenantId() => TenantId;

        public void SetTenantId(Guid tenantId) => TenantId = tenantId;
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

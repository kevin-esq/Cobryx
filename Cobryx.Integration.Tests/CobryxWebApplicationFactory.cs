using System.Data.Common;
using System.Text;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;
using Cobryx.Integration.Tests.Fakes;

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

namespace Cobryx.Integration.Tests;

public class CobryxWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
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
                { "Storage:S3:BucketName", "documents-test" },
                { "Fido2:Origin", "https://test.cobryx.com" },
                { "Fido2:ServerDomain", "test.cobryx.com" },
                { "Security:ClamAV:Host", "localhost" },
                { "Security:ClamAV:Port", "3310" },
                { "Caching:Redis:ConnectionString", "localhost:6379" }
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                string secret = "SuperSecretKeyForTesting!LengthMustBeAtLeast32Chars";
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

            List<ServiceDescriptor> backgroundServices = services.Where(d => d.ServiceType.Name.Contains("IHostedService") || d.ImplementationType?.Name.Contains("Job") == true).ToList();
            foreach (ServiceDescriptor service in backgroundServices)
            {
                services.Remove(service);
            }

            ServiceDescriptor? descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CobryxDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            ServiceDescriptor? emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);

            services.AddSingleton<IEmailService, MockEmailService>();
            services.AddSingleton<MockEmailService>(sp => (MockEmailService)sp.GetRequiredService<IEmailService>());

            ServiceDescriptor? captchaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICaptchaService));
            if (captchaDescriptor != null)
                services.Remove(captchaDescriptor);

            services.AddSingleton<ICaptchaService, MockCaptchaService>();

            services.AddSingleton<IAuthAttemptService, MockAuthAttemptService>();

            ServiceDescriptor? stripeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStripeService));
            if (stripeDescriptor != null)
                services.Remove(stripeDescriptor);

            services.AddSingleton<IStripeService, MockStripeService>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CobryxDbContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<CobryxDbContext>((sp, options) =>
            {
                options.AddInterceptors(
                sp.GetRequiredService<EntityUpdateInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>(),
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<AuditFieldsInterceptor>(),
                sp.GetRequiredService<DbMetricsInterceptor>());
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });


            ServiceDescriptor? tenantDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITenantProvider));
            if (tenantDescriptor != null)
                services.Remove(tenantDescriptor);
            services.AddSingleton<ITenantProvider, TestTenantProvider>();

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Mock Redis-dependent services
            ServiceDescriptor? priorityStoreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICollectionsPriorityStore));
            if (priorityStoreDescriptor != null)
                services.Remove(priorityStoreDescriptor);
            services.AddSingleton<ICollectionsPriorityStore, MockCollectionsPriorityStore>();

            // Replace ICacheService with in-memory implementation
            ServiceDescriptor? cacheServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheServiceDescriptor != null)
                services.Remove(cacheServiceDescriptor);
            services.AddSingleton<ICacheService, MockCacheService>();

            // Mock IConnectionMultiplexer to prevent Redis connection attempts
            ServiceDescriptor? redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer));
            if (redisDescriptor != null)
                services.Remove(redisDescriptor);
        });
    }

    public class TestTenantProvider : ITenantProvider
    {
        public Guid TenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public Guid? GetTenantId() => TenantId;

        public void SetTenantId(Guid tenantId) => TenantId = tenantId;
    }

    public class MockCollectionsPriorityStore : ICollectionsPriorityStore
    {
        public Task<List<PriorityCaseEntry>> GetTopPriorityCasesAsync(Guid tenantId, int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<PriorityCaseEntry>());
        }
    }

    public class MockCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _cache = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out var value) ? (T?)value : default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            _cache[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keysToRemove)
                _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> TryAtomicHashUpdateIfNewerAsync(
            string key,
            IDictionary<string, string> fields,
            long newSequence,
            string sequenceFieldName,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            _cache[key] = fields;
            return Task.FromResult(true);
        }

        public Task<IDictionary<string, string>?> GetHashAllAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out var value)
                ? (IDictionary<string, string>?)value
                : null);
        }
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

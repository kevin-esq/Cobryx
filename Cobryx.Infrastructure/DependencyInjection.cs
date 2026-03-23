using System.Text;

using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.BackgroundJobs;
using Cobryx.Infrastructure.BackgroundJobs.Accounting;
using Cobryx.Infrastructure.BackgroundJobs.Lending;
using Cobryx.Infrastructure.Caching;
using Cobryx.Infrastructure.Configuration;
using Cobryx.Infrastructure.Messaging;
using Cobryx.Infrastructure.Middleware;
using Cobryx.Infrastructure.Modules;
using Cobryx.Infrastructure.MultiTenancy;
using Cobryx.Infrastructure.Observability;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;
using Cobryx.Infrastructure.Repositories;
using Cobryx.Infrastructure.Security;
using Cobryx.Infrastructure.Security.Authorization;
using Cobryx.Infrastructure.Services;
using Cobryx.Infrastructure.Services.Accounting;
using Cobryx.Infrastructure.Services.FileStorage;
using Cobryx.Infrastructure.Services.Notifications;
using Cobryx.Infrastructure.Services.Security;

using Concordia;

using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cobryx.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection("App"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("Email"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Options with startup validation
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<Fido2Options>()
            .Bind(configuration.GetSection(Fido2Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<CaptchaOptions>()
            .Bind(configuration.GetSection(CaptchaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<ClamAvOptions>()
            .Bind(configuration.GetSection(ClamAvOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<CachingOptions>()
            .Bind(configuration.GetSection(CachingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SlackOptions>()
            .Bind(configuration.GetSection(SlackOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddHostedService<ProcessOutboxJob>();
        services.AddTransient<IEmailService, SmtpEmailService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();
        services.AddScoped<IHttpContextService, HttpContextService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IAlertingService, ProductionAlertingService>();

        var cachingConfig = configuration.GetSection(CachingOptions.SectionName)
                                .Get<CachingOptions>()
                            ?? throw new InvalidOperationException("Caching configuration is missing.");

        services.AddDistributedMemoryCache();

        services.AddSingleton<IDistributedCache>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            if (cfg.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                cfg.GetValue<bool>("Caching:UseInMemory"))
            {
                return sp.GetRequiredService<MemoryDistributedCache>();
            }

            // If not testing, use the Redis cache if configured
            CachingOptions cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
            if (string.IsNullOrEmpty(cachingOptions.Redis.ConnectionString))
                return sp.GetRequiredService<MemoryDistributedCache>();
            // Note: This is an internal detail, but IDistributedCache is resolved.
            // Instead of manually constructing RedisCache, we can rely on AddStackExchangeRedisCache
            // but we need to ensure it's registered conditionally.
            // To keep it simple, we'll just return the memory cache if we can't easily switch here,
            // or we check the config earlier if possible.
            // Actually, a better way is to move the whole AddStackExchangeRedisCache call inside an if in Program.cs
            // but we want to keep logic in DependencyInjection.
            var redisOptions = Options.Create(new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
            {
                Configuration = cachingOptions.Redis.ConnectionString,
                InstanceName = "Cobryx_"
            });
            return new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(redisOptions);
        });

        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            if (cfg.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                cfg.GetValue<bool>("Caching:UseInMemory"))
            {
                return new Moq.Mock<StackExchange.Redis.IConnectionMultiplexer>().Object;
            }

            var cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
            return StackExchange.Redis.ConnectionMultiplexer.Connect(cachingOptions.Redis.ConnectionString);
        });

        services.AddSingleton<ICacheService>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
            if (cfg.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                cfg.GetValue<bool>("Caching:UseInMemory"))
            {
                // Use a mock or a memory-based implementation of ICacheService if possible
                // For now, let's keep it simple or use a dummy for testing
                return new RedisCacheService(
                    sp.GetRequiredService<IDistributedCache>(),
                    sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>(),
                    cachingOptions.DefaultTTL);
            }

            return new RedisCacheService(
                sp.GetRequiredService<IDistributedCache>(),
                sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>(),
                cachingConfig.DefaultTTL);
        });

        services.AddScoped<IShadowReplayEngine, ShadowReplayEngine>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<AuditFieldsInterceptor>();
        services.AddScoped<OutboxInterceptor>();
        services.AddScoped<DbMetricsInterceptor>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Logging<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Validation<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Audit<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.UnitOfWork<,>));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            KeepAlive = 30, // Prevent silent terminations by Supabase
            CommandTimeout = 30, // Faster failure for hanging queries
            Pooling = true,
            MinPoolSize = 5, // Reduce cold start latency
            MaxPoolSize = 35, // Balanced for API + Hangfire load
            ConnectionLifetime = 300, // Recycle connections every 5 minutes
            ConnectionIdleLifetime = 60, // Clean up idle connections quickly
            Timeout = 15 // Fail fast if pool is exhausted
        };

        services.AddDbContext<CobryxDbContext>((sp, options) =>
        {
            options.AddInterceptors(
                sp.GetRequiredService<OutboxInterceptor>(),
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<AuditFieldsInterceptor>(),
                sp.GetRequiredService<DbMetricsInterceptor>());

            options.UseNpgsql(npgsqlBuilder.ToString(), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);

                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddScoped<ICobryxDbContext>(sp => sp.GetRequiredService<CobryxDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CobryxDbContext>());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICreditRepository, CreditRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ITaxConfigurationRepository, TaxConfigurationRepository>();
        services.AddScoped<ITenantSubscriptionRepository, TenantSubscriptionRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();

        // Stripe Billing
        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Lending Domain
        services.AddLendingInfrastructure();

        services.AddHttpClient<ISlackService, SlackService>();

        services.AddScoped<Cobryx.Domain.Services.PaymentService>();
        services.AddScoped<Cobryx.Domain.Services.UsageService>();
        services.AddScoped<Cobryx.Domain.Services.DocumentService>();
        services.AddScoped<IDocumentStorage, R2StorageProvider>();
        services.AddScoped<IVirusScanner, ClamAvScanner>();
        services.AddSingleton<ScannerCircuitBreaker>();

        services.AddScoped<IPasswordHasher, Identity.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Identity.JwtTokenGenerator>();
        services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IFido2Service, Fido2Service>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IAuthAttemptService, AuthAttemptService>();

        services.AddScoped<UsageMeteringService>();
        services.AddScoped<IUsageMeteringService>(sp =>
            new CachedUsageMeteringService(
                sp.GetRequiredService<UsageMeteringService>(),
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<Cobryx.Application.Common.Observability.CobryxMetrics>()));
        services.AddScoped<ISubscriptionEnforcementService, SubscriptionEnforcementService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services
            .AddScoped<IGrowthIntelligenceService, Services.Growth.GrowthIntelligenceService>();
        services.AddScoped<IDatabaseDiagnosticService, DatabaseDiagnosticService>();
        services.AddScoped<ConversionDropOffJob>();
        services.AddScoped<ExpirePaymentLinksJob>();
        services.AddScoped<TenantConnectSyncJob>();
        services.AddScoped<FinancialReconciliationJob>();
        services.AddScoped<ReconciliationEngineJob>();
        services.AddScoped<CheckSystemHealthJob>();
        services.AddScoped<LedgerOutboxWorker>();
        services
            .AddScoped<Application.Collections.Assignment.IAssignmentEngine,
                Services.Collections.AssignmentEngine>();
        services
            .AddScoped<Application.Collections.Optimizer.ICollectionOptimizer,
                Services.Collections.CollectionOptimizer>();
        services.AddScoped<DriftDetectionWorker>();
        services.AddScoped<LedgerIntegrityJob>();
        services.AddScoped<LoanAccrualWorker>();
        services.AddScoped<FinancialOutboxWorker>();
        services.AddScoped<RiskAggregationJob>();
        services.AddScoped<Application.ML.ReplayEngine>();

        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (configuration.GetValue<bool>("Hangfire:UseMemoryStorage") ||
                configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                string.IsNullOrEmpty(connectionString))
            {
                config.UseMemoryStorage();
            }
            else
            {
                config.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                }, new PostgreSqlStorageOptions
                {
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    PrepareSchemaIfNecessary = true
                });
            }
        });

        services.AddHangfireServer(options =>
        {
            // Limit workers to ensure we don't exhaust the DB connection pool (MaxPoolSize=35)
            // Reduced to 5 in dev/local to provide more overhead for API and Metrics polling
            options.WorkerCount = 5;
        });

        services.AddHostedService<HangfireMetricsExporter>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName)
                                     .Get<JwtOptions>()
                                 ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies[CobryxClaimTypes.AccessTokenCookieName];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.Configure<Microsoft.AspNetCore.Builder.CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = _ => false;
            options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
            options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        });

        return services;
    }
}

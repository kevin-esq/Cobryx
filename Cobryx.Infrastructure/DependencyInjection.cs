using System.Text;

using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Collections.Assignment;
using Cobryx.Application.Collections.Optimizer;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Decision.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.BackgroundJobs;
using Cobryx.Infrastructure.BackgroundJobs.Accounting;
using Cobryx.Infrastructure.BackgroundJobs.Lending;
using Cobryx.Infrastructure.Caching;
using Cobryx.Infrastructure.Configuration;
using Cobryx.Infrastructure.Decision;
using Cobryx.Infrastructure.Messaging;
using Cobryx.Infrastructure.ML;
using Cobryx.Infrastructure.Modules;
using Cobryx.Infrastructure.MultiTenancy;
using Cobryx.Infrastructure.Observability;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;
using Cobryx.Infrastructure.Providers;
using Cobryx.Infrastructure.Repositories;
using Cobryx.Infrastructure.Security;
using Cobryx.Infrastructure.Security.Authorization;
using Cobryx.Infrastructure.Services;
using Cobryx.Infrastructure.Services.Accounting;
using Cobryx.Infrastructure.Services.Collections;
using Cobryx.Infrastructure.Services.FileStorage;
using Cobryx.Infrastructure.Services.Growth;
using Cobryx.Infrastructure.Services.Notifications;
using Cobryx.Infrastructure.Services.Security;
using Cobryx.Infrastructure.PipelineBehaviors;

using Concordia;

using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using StackExchange.Redis;

namespace Cobryx.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            RegisterOptions(services, configuration);
            RegisterCoreServices(services);
            RegisterCaching(services);
            RegisterInterceptors(services);
            RegisterPipelineBehaviors(services);
            RegisterDatabase(services, configuration);
            RegisterRepositories(services);
            RegisterApplicationServices(services);
            RegisterBackgroundJobs(services, configuration);
            RegisterAuthentication(services, configuration);
            RegisterCookiePolicy(services);

            // ML Infrastructure (feature stores, clients, routing)
            services.AddMlInfrastructure();

            return services;
        }

        private static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
        {
            _ = services.AddOptions<AppOptions>()
                .Bind(configuration.GetSection("App"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("Email"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<Fido2Options>()
                .Bind(configuration.GetSection(Fido2Options.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<CaptchaOptions>()
                .Bind(configuration.GetSection(CaptchaOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<ClamAvOptions>()
                .Bind(configuration.GetSection(ClamAvOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<CachingOptions>()
                .Bind(configuration.GetSection(CachingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<SlackOptions>()
                .Bind(configuration.GetSection(SlackOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<StripeOptions>()
                .Bind(configuration.GetSection(StripeOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<ShadowConfig>()
                .Bind(configuration.GetSection(ShadowConfig.SectionName))
                .ValidateDataAnnotations();

            _ = services.AddOptions<S3StorageOptions>()
                .Bind(configuration.GetSection(S3StorageOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddOptions<GoogleOAuthOptions>()
                .Bind(configuration.GetSection(GoogleOAuthOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            _ = services.AddSingleton<IClock, SystemClock>();
            _ = services.AddSingleton<IFeatureFlags, FeatureFlagsService>();
            _ = services.AddHttpContextAccessor();
            _ = services.AddScoped<ITenantProvider, TenantProvider>();
            _ = services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            _ = services.AddHostedService<Messaging.ProcessOutboxJob>();
            _ = services.AddHostedService<BackgroundJobs.Decision.SnapshotBackgroundWorker>();
            _ = services.AddScoped<BackgroundJobs.Decision.RegressionSuiteJob>();
            _ = services.AddTransient<IEmailService, SmtpEmailService>();
            _ = services.AddTransient<IExternalAuthService, ExternalAuthService>();
            _ = services.AddScoped<IHttpContextService, HttpContextService>();
            _ = services.AddScoped<ICookieService, CookieService>();
            _ = services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
            _ = services.AddScoped<IAlertingService, ProductionAlertingService>();
            _ = services.AddScoped<IShadowMonitor, ShadowMonitor>();
            _ = services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            _ = services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        }

        private static void RegisterCaching(IServiceCollection services)
        {
            _ = services.AddDistributedMemoryCache();
            _ = services.AddMemoryCache();

            _ = services.AddSingleton<IDistributedCache>(static sp =>
            {
                if (IsInMemoryMode(sp))
                {
                    return sp.GetRequiredService<MemoryDistributedCache>();
                }

                CachingOptions options = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
                return string.IsNullOrEmpty(options.Redis.ConnectionString)
                    ? sp.GetRequiredService<MemoryDistributedCache>()
                    : new RedisCache(Options.Create(new RedisCacheOptions
                    {
                        Configuration = options.Redis.ConnectionString,
                        InstanceName = "Cobryx_"
                    }));
            });

            _ = services.AddSingleton<IConnectionMultiplexer>(static sp =>
            {
                if (IsInMemoryMode(sp))
                {
                    return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
                }

                CachingOptions options = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
                return ConnectionMultiplexer.Connect(options.Redis.ConnectionString);
            });

            _ = services.AddScoped<ICacheService>(static sp =>
            {
                CachingOptions options = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
                return new RedisCacheService(
                    sp.GetRequiredService<IDistributedCache>(),
                    sp.GetRequiredService<IConnectionMultiplexer>(),
                    options.DefaultTTL);
            });
        }

        private static void RegisterInterceptors(IServiceCollection services)
        {
            _ = services.AddScoped<AuditInterceptor>();
            _ = services.AddScoped<AuditFieldsInterceptor>();
            _ = services.AddScoped<OutboxInterceptor>();
            _ = services.AddScoped<DbMetricsInterceptor>();
            _ = services.AddScoped<EntityUpdateInterceptor>();
        }

        private static void RegisterPipelineBehaviors(IServiceCollection services)
        {
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Middleware.PipelineBehaviors.Logging<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyBehavior<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Middleware.PipelineBehaviors.Validation<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Middleware.PipelineBehaviors.TenantValidation<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Middleware.PipelineBehaviors.Audit<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Middleware.PipelineBehaviors.UnitOfWork<,>));
        }

        private static void RegisterDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
            {
                KeepAlive = 30,
                CommandTimeout = 30,
                Pooling = true,
                MinPoolSize = 5,
                MaxPoolSize = 35,
                ConnectionLifetime = 300,
                ConnectionIdleLifetime = 60,
                Timeout = 15
            };

            _ = services.AddDbContext<CobryxDbContext>((sp, options) =>
            {
                _ = options.AddInterceptors(
                    sp.GetRequiredService<EntityUpdateInterceptor>(),
                    sp.GetRequiredService<OutboxInterceptor>(),
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<AuditFieldsInterceptor>(),
                    sp.GetRequiredService<DbMetricsInterceptor>(),
                    sp.GetRequiredService<EntityUpdateInterceptor>());

                _ = options.UseNpgsql(npgsqlBuilder.ToString(), npgsql =>
                {
                    _ = npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);

                    _ = npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            });

            _ = services.AddScoped<ICobryxDbContext>(sp => sp.GetRequiredService<CobryxDbContext>());
            _ = services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CobryxDbContext>());
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            _ = services.AddScoped<ICustomerRepository, CustomerRepository>();
            _ = services.AddScoped<IProductRepository, ProductRepository>();
            _ = services.AddScoped<ICreditRepository, CreditRepository>();
            _ = services.AddScoped<ITenantRepository, TenantRepository>();
            _ = services.AddScoped<IUserRepository, UserRepository>();
            _ = services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            _ = services.AddScoped<IRoleRepository, RoleRepository>();
            _ = services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
            _ = services.AddScoped<ITaxConfigurationRepository, TaxConfigurationRepository>();
            _ = services.AddScoped<ITenantSubscriptionRepository, TenantSubscriptionRepository>();
            _ = services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            _ = services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
            _ = services.AddScoped<INotificationRepository, NotificationRepository>();
        }

        private static void RegisterApplicationServices(IServiceCollection services)
        {
            _ = services.AddLendingInfrastructure();
            _ = services.AddHttpClient<ISlackService, SlackService>();
            _ = services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

            _ = services.AddScoped<Domain.Services.PaymentService>();
            _ = services.AddScoped<Domain.Services.UsageService>();
            _ = services.AddScoped<Domain.Services.DocumentService>();
            _ = services.AddScoped<IDocumentStorage, R2StorageProvider>();
            _ = services.AddScoped<IVirusScanner, ClamAvScanner>();
            _ = services.AddSingleton<ScannerCircuitBreaker>();

            _ = services.AddScoped<IPasswordHasher, Identity.PasswordHasher>();
            _ = services.AddScoped<IJwtTokenGenerator, Identity.JwtTokenGenerator>();
            _ = services.AddSingleton<ILedgerHealthCache, LedgerHealthCache>();
            _ = services.AddSingleton<ILedgerHasher, LedgerHasher>();
            _ = services.AddSingleton<ILedgerSigner, HmacLedgerSigner>();
            _ = services.AddSingleton<ILedgerAnchorStore, ChainedFileAnchorStore>();
            _ = services.AddScoped<ILedgerAnchorService, LedgerAnchorService>();
            _ = services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
            _ = services.AddScoped<IIdempotencyStore, Idempotency.IdempotencyStore>();
            _ = services.AddScoped<IProcessedWebhookEventRepository, Persistence.Repositories.ProcessedWebhookEventRepository>();
            _ = services.AddScoped<IMfaService, MfaService>();
            _ = services.AddScoped<IFido2Service, Fido2Service>();
            _ = services.AddScoped<ISecurityAuditService, SecurityAuditService>();
            _ = services.AddScoped<IAuthAttemptService, AuthAttemptService>();
            _ = services.AddScoped<IRandomProvider, SystemRandomProvider>();
            // ModelRouter is now registered via MlInfrastructureModule
            _ = services.AddScoped<IShadowReplayEngine, ShadowReplayEngine>();

            _ = services.AddScoped<UsageMeteringService>();
            _ = services.AddScoped<IUsageMeteringService>(static sp => new CachedUsageMeteringService(
                sp.GetRequiredService<UsageMeteringService>(),
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<CobryxMetrics>()));

            _ = services.AddScoped<ISubscriptionEnforcementService, SubscriptionEnforcementService>();
            _ = services.AddScoped<IPermissionService, PermissionService>();
            _ = services.AddScoped<IGrowthIntelligenceService, GrowthIntelligenceService>();
            _ = services.AddScoped<IDatabaseDiagnosticService, DatabaseDiagnosticService>();
            _ = services.AddScoped<IAssignmentEngine, AssignmentEngine>();
            _ = services.AddScoped<ICollectionOptimizer, CollectionOptimizer>();

            // ML & Decision Services
            _ = services.AddScoped<IDriftAnalyzer, Application.Decision.DriftAnalyzer>();
            _ = services.AddScoped<ICollectionsPriorityStore, RedisCollectionsPriorityStore>();
        }

        private static void RegisterBackgroundJobs(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            _ = services.AddScoped<ConversionDropOffJob>();
            _ = services.AddScoped<ExpirePaymentLinksJob>();
            _ = services.AddScoped<TenantConnectSyncJob>();
            _ = services.AddScoped<FinancialReconciliationJob>();
            _ = services.AddScoped<ReconciliationEngineJob>();
            _ = services.AddScoped<CheckSystemHealthJob>();
            _ = services.AddScoped<LedgerOutboxWorker>();
            _ = services.AddScoped<DriftDetectionWorker>();
            _ = services.AddScoped<LedgerIntegrityJob>();
            _ = services.AddScoped<LoanAccrualWorker>();
            _ = services.AddScoped<FinancialOutboxWorker>();
            _ = services.AddScoped<RiskAggregationJob>();
            _ = services.AddScoped<Application.ML.ReplayEngine>();

            _ = services.AddHangfire(config =>
            {
                _ = config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings();

                if (configuration.GetValue<bool>("Hangfire:UseMemoryStorage") ||
                    configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                    string.IsNullOrEmpty(connectionString))
                {
                    _ = config.UseMemoryStorage();
                }
                else
                {
                    _ = config.UsePostgreSqlStorage(
                        options => options.UseNpgsqlConnection(connectionString),
                        new PostgreSqlStorageOptions
                        {
                            JobExpirationCheckInterval = TimeSpan.FromHours(1),
                            PrepareSchemaIfNecessary = true
                        });
                }
            });

            _ = services.AddHangfireServer(options => options.WorkerCount = 5);
            _ = services.AddHostedService<HangfireMetricsExporter>();
        }

        private static void RegisterAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            _ = services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    JwtOptions jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                                            ?? throw new InvalidOperationException(
                                                "JwtSettings configuration is missing.");

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
        }

        private static void RegisterCookiePolicy(IServiceCollection services)
        {
            _ = services.Configure<CookiePolicyOptions>(static options =>
            {
                options.CheckConsentNeeded = static _ => false;
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
                options.HttpOnly = HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.Always;
            });
        }

        private static bool IsInMemoryMode(IServiceProvider sp)
        {
            IConfiguration cfg = sp.GetRequiredService<IConfiguration>();
            return cfg.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing"
                   || cfg.GetValue<bool>("Caching:UseInMemory");
        }
    }
}

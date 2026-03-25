using System.Text;

using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML;
using Cobryx.Application.Decision.Interfaces;
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
using Cobryx.Infrastructure.Middleware;
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

namespace Cobryx.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
            IConfiguration configuration)
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

            _ = services.AddOptions<Application.Decision.Models.ShadowConfig>()
                .Bind(configuration.GetSection(Application.Decision.Models.ShadowConfig.SectionName))
                .ValidateDataAnnotations();

            _ = services.AddSingleton<IClock, SystemClock>();

            _ = services.AddHttpContextAccessor();
            _ = services.AddScoped<ITenantProvider, TenantProvider>();
            _ = services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            _ = services.AddHostedService<ProcessOutboxJob>();
            _ = services.AddHostedService<BackgroundJobs.Decision.SnapshotBackgroundWorker>();
            _ = services.AddScoped<BackgroundJobs.Decision.RegressionSuiteJob>();

            _ = services.AddTransient<IEmailService, SmtpEmailService>();
            _ = services.AddTransient<IExternalAuthService, ExternalAuthService>();
            _ = services.AddScoped<IHttpContextService, HttpContextService>();
            _ = services.AddScoped<ICookieService, CookieService>();
            _ = services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
            _ = services.AddScoped<IAlertingService, ProductionAlertingService>();
            _ = services.AddScoped<IShadowMonitor, ShadowMonitor>();

            var cachingConfig = configuration.GetSection(CachingOptions.SectionName)
                                    .Get<CachingOptions>()
                                ?? throw new InvalidOperationException("Caching configuration is missing.");

            _ = services.AddDistributedMemoryCache();

            _ = services.AddSingleton<IDistributedCache>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                if (cfg.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                    cfg.GetValue<bool>("Caching:UseInMemory"))
                {
                    return sp.GetRequiredService<MemoryDistributedCache>();
                }

                CachingOptions cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
                if (string.IsNullOrEmpty(cachingOptions.Redis.ConnectionString))
                {
                    return sp.GetRequiredService<MemoryDistributedCache>();
                }

                var redisOptions = Options.Create(new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
                {
                    Configuration = cachingOptions.Redis.ConnectionString,
                    InstanceName = "Cobryx_"
                });
                return new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(redisOptions);
            });

            _ = services.AddSingleton(sp =>
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

            _ = services.AddSingleton<ICacheService>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
                return cfg.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                       cfg.GetValue<bool>("Caching:UseInMemory")
                    ? new RedisCacheService(
                        sp.GetRequiredService<IDistributedCache>(),
                        sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>(),
                        cachingOptions.DefaultTTL)
                    : new RedisCacheService(
                        sp.GetRequiredService<IDistributedCache>(),
                        sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>(),
                        cachingConfig.DefaultTTL);
            });

            _ = services.AddScoped<IShadowReplayEngine, ShadowReplayEngine>();
            _ = services.AddScoped<AuditInterceptor>();
            _ = services.AddScoped<AuditFieldsInterceptor>();
            _ = services.AddScoped<OutboxInterceptor>();
            _ = services.AddScoped<DbMetricsInterceptor>();

            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Logging<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Validation<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Audit<,>));
            _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.UnitOfWork<,>));

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
                    sp.GetRequiredService<OutboxInterceptor>(),
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<AuditFieldsInterceptor>(),
                    sp.GetRequiredService<DbMetricsInterceptor>());

                _ = options.UseNpgsql(npgsqlBuilder.ToString(), npgsqlOptions =>
                {
                    _ = npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);

                    _ = npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            });

            _ = services.AddScoped<ICobryxDbContext>(sp => sp.GetRequiredService<CobryxDbContext>());
            _ = services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CobryxDbContext>());

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

            _ = services.AddOptions<StripeOptions>()
                .Bind(configuration.GetSection(StripeOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            _ = services.AddLendingInfrastructure();

            _ = services.AddHttpClient<ISlackService, SlackService>();

            _ = services.AddScoped<Domain.Services.PaymentService>();
            _ = services.AddScoped<Domain.Services.UsageService>();
            _ = services.AddScoped<Domain.Services.DocumentService>();
            _ = services.AddScoped<IDocumentStorage, R2StorageProvider>();
            _ = services.AddScoped<IVirusScanner, ClamAvScanner>();
            _ = services.AddSingleton<ScannerCircuitBreaker>();

            _ = services.AddScoped<IPasswordHasher, Identity.PasswordHasher>();
            _ = services.AddScoped<IJwtTokenGenerator, Identity.JwtTokenGenerator>();
            _ = services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
            _ = services.AddScoped<IMfaService, MfaService>();
            _ = services.AddScoped<IFido2Service, Fido2Service>();
            _ = services.AddScoped<ISecurityAuditService, SecurityAuditService>();
            _ = services.AddScoped<IAuthAttemptService, AuthAttemptService>();
            _ = services.AddScoped<IRandomProvider, SystemRandomProvider>();
            _ = services.AddScoped<ModelRouter>();

            _ = services.AddScoped<UsageMeteringService>();
            _ = services.AddScoped<IUsageMeteringService>(sp =>
                new CachedUsageMeteringService(
                    sp.GetRequiredService<UsageMeteringService>(),
                    sp.GetRequiredService<ICacheService>(),
                    sp.GetRequiredService<Application.Common.Observability.CobryxMetrics>()));
            _ = services.AddScoped<ISubscriptionEnforcementService, SubscriptionEnforcementService>();
            _ = services.AddScoped<IPermissionService, PermissionService>();
            _ = services
                .AddScoped<IGrowthIntelligenceService, Services.Growth.GrowthIntelligenceService>();
            _ = services.AddScoped<IDatabaseDiagnosticService, DatabaseDiagnosticService>();
            _ = services.AddScoped<ConversionDropOffJob>();
            _ = services.AddScoped<ExpirePaymentLinksJob>();
            _ = services.AddScoped<TenantConnectSyncJob>();
            _ = services.AddScoped<FinancialReconciliationJob>();
            _ = services.AddScoped<ReconciliationEngineJob>();
            _ = services.AddScoped<CheckSystemHealthJob>();
            _ = services.AddScoped<LedgerOutboxWorker>();
            _ = services
                .AddScoped<Application.Collections.Assignment.IAssignmentEngine,
                    Services.Collections.AssignmentEngine>();
            _ = services
                .AddScoped<Application.Collections.Optimizer.ICollectionOptimizer,
                    Services.Collections.CollectionOptimizer>();
            _ = services.AddScoped<DriftDetectionWorker>();
            _ = services.AddScoped<LedgerIntegrityJob>();
            _ = services.AddScoped<LoanAccrualWorker>();
            _ = services.AddScoped<FinancialOutboxWorker>();
            _ = services.AddScoped<RiskAggregationJob>();
            _ = services.AddScoped<ReplayEngine>();

            _ = services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
            _ = services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

            _ = services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

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
                    _ = config.UsePostgreSqlStorage(options =>
                    {
                        _ = options.UseNpgsqlConnection(connectionString);
                    }, new PostgreSqlStorageOptions
                    {
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        PrepareSchemaIfNecessary = true
                    });
                }
            });

            _ = services.AddHangfireServer(options =>
            {
                options.WorkerCount = 5;
            });

            _ = services.AddHostedService<HangfireMetricsExporter>();

            _ = services.AddAuthentication(options =>
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

            _ = services.Configure<Microsoft.AspNetCore.Builder.CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = _ => false;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            });

            return services;
        }
    }
}

using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Configuration;
using Amazon.S3;
using Cobryx.Application.Common.Configuration;
using Cobryx.Infrastructure.Persistence.Interceptors;
using Cobryx.Infrastructure.Repositories;
using Cobryx.Infrastructure.MultiTenancy;
using Cobryx.Infrastructure.Middleware;
using Cobryx.Infrastructure.Services;
using Cobryx.Domain.Interfaces;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Infrastructure.HealthChecks;
using Cobryx.Infrastructure.Identity;
using Cobryx.Infrastructure.Services.FileStorage;
using Cobryx.Infrastructure.Services.Security;
using Concordia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;
using Cobryx.Infrastructure.Caching;
using Cobryx.Infrastructure.Security;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Infrastructure.Webhooks.Stripe;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Cobryx.Infrastructure.Security.Authorization;

using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Domain.DomainServices.Lending;
using Cobryx.Infrastructure.Repositories.Lending;

namespace Cobryx.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Cobryx.Application.Common.Configuration.AppOptions>()
            .Bind(configuration.GetSection("App"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("Email"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Options with startup validation
        services.AddOptions<Configuration.JwtOptions>()
            .Bind(configuration.GetSection(Configuration.JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<Configuration.Fido2Options>()
            .Bind(configuration.GetSection(Configuration.Fido2Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<Configuration.CaptchaOptions>()
            .Bind(configuration.GetSection(Configuration.CaptchaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<Configuration.ClamAvOptions>()
            .Bind(configuration.GetSection(Configuration.ClamAvOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<Configuration.CachingOptions>()
            .Bind(configuration.GetSection(Configuration.CachingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddHostedService<BackgroundJobs.ProcessOutboxJob>();
        services.AddTransient<IEmailService, SmtpEmailService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();
        services.AddScoped<IHttpContextService, Services.HttpContextService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IAuditLogQueryService, Services.AuditLogQueryService>();

        var cachingConfig = configuration.GetSection(Configuration.CachingOptions.SectionName).Get<Configuration.CachingOptions>()
            ?? throw new InvalidOperationException("Caching configuration is missing.");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = cachingConfig.Redis.ConnectionString;
            options.InstanceName = "Cobryx_";
        });

        services.AddSingleton<ICacheService>(sp =>
            new RedisCacheService(sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(), cachingConfig.DefaultTTL));

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Logging<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Validation<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Audit<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.UnitOfWork<,>));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            KeepAlive = 30,
            CommandTimeout = 300,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 100
        };

        services.AddDbContext<CobryxDbContext>((sp, options) =>
        {
            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>());

            options.UseNpgsql(npgsqlBuilder.ToString(), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);

                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CobryxDbContext>());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICreditRepository, CreditRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ITaxConfigurationRepository, TaxConfigurationRepository>();
        services.AddScoped<ITenantSubscriptionRepository, TenantSubscriptionRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();

        // Stripe Billing
        services.AddOptions<Configuration.StripeOptions>()
            .Bind(configuration.GetSection(Configuration.StripeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IStripeService, Payments.Stripe.StripeService>();
        services.AddScoped<Application.Subscriptions.Services.StripeSubscriptionSyncService>();

        // Lending Domain Repositories
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanAgreementRepository, LoanAgreementRepository>();
        services.AddScoped<IInstallmentRepository, InstallmentRepository>();
        services.AddScoped<ICreditSaleRepository, CreditSaleRepository>();
        services.AddScoped<IInterestPolicyRepository, PolicyRepository>();
        services.AddScoped<ILateFeePolicyRepository, PolicyRepository>();
        services.AddScoped<IPaymentApplicationPolicyRepository, PolicyRepository>();

        // Lending Domain Services
        services.AddScoped<IAmortizationService, AmortizationService>();
        services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();

        services.AddScoped<Cobryx.Domain.Services.PaymentService>();
        services.AddScoped<Cobryx.Domain.Services.UsageService>();
        services.AddScoped<Cobryx.Domain.Services.DocumentService>();
        services.AddScoped<IDocumentStorage, R2StorageProvider>();
        services.AddScoped<IVirusScanner, Services.Security.ClamAvScanner>();
        services.AddSingleton<Services.Security.ScannerCircuitBreaker>();

        services.AddScoped<IPasswordHasher, Identity.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Identity.JwtTokenGenerator>();
        services.AddScoped<IInvoiceNumberService, Services.InvoiceNumberService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IFido2Service, Fido2Service>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IAuthAttemptService, AuthAttemptService>();
        services.AddScoped<IWebhookParser, StripeWebhookParser>();

        services.AddScoped<UsageMeteringService>();
        services.AddScoped<IUsageMeteringService>(sp =>
            new CachedUsageMeteringService(
                sp.GetRequiredService<UsageMeteringService>(),
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<Cobryx.Application.Common.Observability.CobryxMetrics>()));
        services.AddScoped<ISubscriptionEnforcementService, SubscriptionEnforcementService>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                }, new PostgreSqlStorageOptions
                {
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    PrepareSchemaIfNecessary = true
                });
        });

        services.AddHangfireServer();

        var jwtConfig = configuration.GetSection(Configuration.JwtOptions.SectionName).Get<Configuration.JwtOptions>()
            ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
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
            options.CheckConsentNeeded = context => false;
            options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
            options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        });

        return services;
    }
}

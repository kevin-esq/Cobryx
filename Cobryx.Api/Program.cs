using Asp.Versioning;

using Cobryx.Application;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Modules;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure;
using Cobryx.Infrastructure.HealthChecks;
using Cobryx.Infrastructure.Modules;

using Hangfire;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

using OpenTelemetry.Metrics;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Cobryx.Api")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton<CobryxMetrics>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(CobryxMetrics.MeterName);
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddPrometheusExporter();
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Cobryx API",
        Version = "v1",
        Description = "Cobryx Financial Platform API. Organized by business domains: Identity, Financial Core, and System Administration."
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"{token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.CustomSchemaIds(type => GetSchemaId(type));

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    var appXmlFile = "Cobryx.Application.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
    if (File.Exists(appXmlPath))
    {
        c.IncludeXmlComments(appXmlPath, includeControllerXmlComments: true);
    }

    c.EnableAnnotations();
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Cobryx.Api.Infrastructure.SessionValidationFilter>();
    options.Filters.Add<Cobryx.Api.Infrastructure.Observability.ObservabilityFilter>();
    options.Filters.Add<Cobryx.Api.Infrastructure.IdempotencyKeyFilter>();
    options.Filters.Add<Cobryx.Api.Infrastructure.PlanGatingFilter>();
})
.AddJsonOptions(json =>
{
    json.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddCobryxHealthChecks(builder.Configuration);

builder.Services.AddSingleton<Cobryx.Application.Documents.Services.FileSignatureValidator>();
builder.Services.AddScoped<Cobryx.Application.Documents.Commands.ScanDocument.ScanDocumentHandler>();
builder.Services.AddScoped<Cobryx.Infrastructure.BackgroundJobs.CleanupStaleDocumentsJob>();

builder.Services
    .AddApplicationServices()
    .AddLendingModule()
    .AddPaymentsModule()
    .AddAccountingModule()
    .AddInfrastructureServices(builder.Configuration)
    .AddLendingInfrastructure()
    .AddPaymentsInfrastructure()
    .AddAccountingInfrastructure();

builder.Services.AddExceptionHandler<Cobryx.Api.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanViewCustomers", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "customers:view"))
    .AddPolicy("CanCreateCustomers", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "customers:create"))
    .AddPolicy("CanViewCredits", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "credits:view"))
    .AddPolicy("CanCreateCredits", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "credits:create"))
    .AddPolicy("CanApplyPayments", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "payments:apply"))
    .AddPolicy("CanManageTenant", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "tenant:manage"))
    .AddPolicy("PlatformAdmin", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "platform:admin"))
    .AddPolicy("EmailVerified", policy => policy.RequireClaim(CobryxClaimTypes.EmailVerified, "true"))
    .AddPolicy("AccountVerified", policy =>
        policy.RequireClaim(CobryxClaimTypes.EmailVerified, "true")
              .RequireClaim(CobryxClaimTypes.RequiresOnboarding, "false"));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("X-Api-Version"),
        new UrlSegmentApiVersionReader());
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var appOptions = builder.Configuration.GetSection("App").Get<Cobryx.Application.Common.Configuration.AppOptions>() ?? new Cobryx.Application.Common.Configuration.AppOptions();
        policy.WithOrigins(appOptions.AppUrl)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.Use((context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/metrics"))
    {
        var metricsKey = builder.Configuration["METRICS_SECRET_KEY"];
        if (string.IsNullOrEmpty(metricsKey) || context.Request.Headers["X-Metrics-Key"] != metricsKey)
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
    }
    return next();
});

app.UseOpenTelemetryPrometheusScrapingEndpoint();

var enableSwagger = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("ENABLE_SWAGGER");

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<Cobryx.Api.Middlewares.CorrelationIdMiddleware>();
app.UseMiddleware<Cobryx.Infrastructure.Middleware.RequestLogContextMiddleware>();
app.UseMiddleware<Cobryx.Infrastructure.Middleware.DynamicRateLimitingMiddleware>();
app.UseRateLimiter();
app.UseCors("DefaultCors");

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; object-src 'none';");
    await next();
});

app.UseCookiePolicy();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<Cobryx.Api.Middlewares.TenantMiddleware>();
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseMiddleware<Cobryx.Api.Middlewares.SubscriptionGateMiddleware>();
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Cobryx Jobs Manager",
    Authorization = [new Cobryx.Api.Infrastructure.HangfireDashboardFilter()]
});


app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.UseOpenTelemetryPrometheusScrapingEndpoint();

try
{
    using var scope = app.Services.CreateScope();
    {
        var roleRepo = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Cobryx.Infrastructure.Persistence.CobryxDbContext>();

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") || Environment.GetEnvironmentVariable("ENABLE_MIGRATION") == "true")
        {
            if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                await dbContext.Database.EnsureCreatedAsync();
                Log.Information("SQLite database created from model (skipping PG-specific migrations)");
            }
            else
            {
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migration completed successfully");
            }
        }

        await Cobryx.Infrastructure.Persistence.DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
        await Cobryx.Infrastructure.Persistence.DbInitializer.SeedPlansAsync(dbContext);
        await Cobryx.Infrastructure.Persistence.DbInitializer.SeedPlatformTenantAsync(dbContext);
        Log.Information("Database seeding completed successfully");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.Messaging.ProcessOutboxJob>(
            "process-outbox-events",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.Messaging.LedgerOutboxWorker>(
            "ledger-cdc-outbox",
            job => job.ProcessEventsAsync(CancellationToken.None),
            "*/5 * * * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.Accounting.LedgerIntegrityJob>(
            "ledger-integrity-scan",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.Accounting.DriftDetectionWorker>(
            "ledger-drift-detection",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely);

        RecurringJob.AddOrUpdate<Cobryx.Application.Analytics.Jobs.PortfolioMetricsJob>(
            "portfolio-metrics",
            job => job.RunAsync(),
            Cron.Daily(0));

        RecurringJob.AddOrUpdate<Cobryx.Application.Analytics.Jobs.PortfolioCacheRefreshJob>(
            "portfolio-cache-refresh",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(0, 30));

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.Lending.LoanAccrualWorker>(
            "loan-daily-accrual",
            job => job.ExecuteAsync(),
            "0 1 * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.Collections.CollectionsOrchestratorJob>(
            "collections-orchestrator",
            job => job.ProcessCollectionsAsync(),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.Collections.CollectionsOptimizerJob>(
            "collections-ai-optimizer",
            job => job.RunHourlyOptimizationAsync(),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Cobryx.Application.Risk.Jobs.EarlyWarningJob>(
            "risk-early-warning",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Cobryx.Application.ML.Jobs.DatasetExporterJob>(
            "ml-dataset-export",
            job => job.RunAsync(),
            Cron.Daily);

        RecurringJob.AddOrUpdate<Cobryx.Application.ML.Jobs.RlTrainingJob>(
            "rl-training",
            job => job.RunAsync(),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.TenantConnectSyncJob>(
            "stripe-connect-sync",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.FinancialReconciliationJob>(
            "financial-reconciliation-recovery",
            job => job.RunAsync(CancellationToken.None),
            "*/30 * * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.CleanupStaleDocumentsJob>(
            "cleanup-stale-documents",
            job => job.RunAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.InvitationCleanupJob>(
            "invitation-cleanup",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.ExpirePaymentLinksJob>(
            "payment-link-expiration",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *");

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.PaymentReminderJob>(
            "payment-collections-reminders",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily);

        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.CheckSystemHealthJob>(
            "system-health-check",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to seed database.");
}

Log.Information("Starting web host...");
app.Run();
Log.Information("Web host stopped");

static string GetSchemaId(Type type)
{
    if (!type.IsGenericType)
    {
        if (type.Namespace != null && type.Namespace.StartsWith("Cobryx.Domain"))
        {
            var suffix = type.Namespace
                .Replace("Cobryx.Domain.", "")
                .Replace(".", "_");
            return $"{suffix}_{type.Name}";
        }
        return type.Name;
    }
    var genericName = type.Name.Split('`')[0];
    var genericArgs = string.Join("Of", type.GetGenericArguments().Select(GetSchemaId));
    return $"{genericName}{genericArgs}";
}

public partial class Program { }

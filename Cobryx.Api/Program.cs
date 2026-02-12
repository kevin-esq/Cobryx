using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Cobryx.Application;
using Cobryx.Infrastructure;
using Cobryx.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Cobryx.Infrastructure.Configuration;
using Cobryx.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Cobryx.Application.Common.Observability;
using OpenTelemetry.Metrics;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using Hangfire;

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
    c.IncludeXmlComments(xmlPath);

    var appXmlFile = "Cobryx.Application.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
    if (File.Exists(appXmlPath))
    {
        c.IncludeXmlComments(appXmlPath);
    }
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Cobryx.Api.Infrastructure.SessionValidationFilter>();
    options.Filters.Add<Cobryx.Api.Infrastructure.Observability.ObservabilityFilter>();
    options.Filters.Add<Cobryx.Api.Infrastructure.IdempotencyKeyFilter>();
})
.AddJsonOptions(json =>
{
    json.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddCobryxHealthChecks(builder.Configuration);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewCustomers", policy => policy.RequireClaim("permissions", "customers:view"));
    options.AddPolicy("CanCreateCustomers", policy => policy.RequireClaim("permissions", "customers:create"));
    options.AddPolicy("CanViewCredits", policy => policy.RequireClaim("permissions", "credits:view"));
    options.AddPolicy("CanCreateCredits", policy => policy.RequireClaim("permissions", "credits:create"));
    options.AddPolicy("CanApplyPayments", policy => policy.RequireClaim("permissions", "payments:apply"));
    options.AddPolicy("CanManageTenant", policy => policy.RequireClaim("permissions", "tenant:manage"));
    options.AddPolicy("EmailVerified", policy => policy.RequireClaim("email_verified", "true"));
    options.AddPolicy("AccountVerified", policy =>
        policy.RequireClaim("email_verified", "true")
              .RequireClaim("requires_onboarding", "false"));
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("X-Api-Version");
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
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

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Cobryx Jobs Manager",
    Authorization = new[] { new Cobryx.Api.Infrastructure.HangfireDashboardFilter() }
});

// Cloud Run terminates TLS — no HTTPS redirect needed in container.

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

try
{
    using (var scope = app.Services.CreateScope())
    {
        var roleRepo = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Cobryx.Infrastructure.Persistence.CobryxDbContext>();

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") || Environment.GetEnvironmentVariable("ENABLE_MIGRATION") == "true")
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migration completed successfully");
        }

        await Cobryx.Infrastructure.Persistence.DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
        await Cobryx.Infrastructure.Persistence.DbInitializer.SeedPlansAsync(dbContext);
        Log.Information("Database seeding completed successfully");

        // Register Recurring Jobs
        RecurringJob.AddOrUpdate<Cobryx.Infrastructure.BackgroundJobs.ProcessOutboxJob>(
            "process-outbox-events",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * * *"); // Every 10 seconds
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

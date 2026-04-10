using Cobryx.Api.Extensions;
using Cobryx.Api.Filters;
using Cobryx.Application;
using Cobryx.Application.Documents.Commands.ScanDocument;
using Cobryx.Application.Documents.Services;
using Cobryx.Application.Modules;
using Cobryx.Infrastructure;
using Cobryx.Infrastructure.BackgroundJobs;
using Cobryx.Infrastructure.HealthChecks;
using Cobryx.Infrastructure.Modules;

using Serilog;

// ============================================
// BUILDER CONFIGURATION
// ============================================

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Cobryx.Api")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Core Services
builder.Services
    .AddCobryxOpenTelemetry()
    .AddCobryxSwagger()
    .AddCobryxControllers()
    .AddCobryxHealthChecks(builder.Configuration)
    .AddCobryxRateLimiting()
    .AddCobryxAuthorization()
    .AddCobryxApiVersioning()
    .AddCobryxCors(builder.Configuration);

// Document Services
builder.Services.AddSingleton<FileSignatureValidator>();
builder.Services.AddScoped<ScanDocumentHandler>();
builder.Services.AddScoped<CleanupStaleDocumentsJob>();

// Application & Infrastructure
builder.Services
    .AddApplicationServices()
    .AddLendingModule()
    .AddPaymentsModule()
    .AddAccountingModule()
    .AddInfrastructureServices(builder.Configuration)
    .AddLendingInfrastructure()
    .AddPaymentsInfrastructure()
    .AddAccountingInfrastructure();

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ============================================
// APPLICATION PIPELINE
// ============================================

WebApplication app = builder.Build();

app.UseCobryxMiddleware(builder.Configuration);
app.UseCobryxEndpoints();

// ============================================
// STARTUP INITIALIZATION
// ============================================

try
{
    await app.InitializeDatabaseAsync();
    app.Services.RegisterCobryxRecurringJobs();
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to initialize application");
}

// ============================================
// RUN
// ============================================

Log.Information("Starting Cobryx API...");
app.Run();
Log.Information("Cobryx API stopped");

public partial class Program;

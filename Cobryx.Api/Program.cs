using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Cobryx.Application;
using Cobryx.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 📊 Configure Serilog (Structured Logging)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Cobryx.Api")
    .WriteTo.Console()
    .WriteTo.File("logs/cobryx-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// 🏥 Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Cobryx.Infrastructure.Persistence.CobryxDbContext>("Database");

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

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
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins("https://app.cobryx.com.mx") 
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 🚀 Production Hardening Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseCors("DefaultCors");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<Cobryx.Api.Middlewares.GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<Cobryx.Api.Middlewares.TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

// 🏥 Map Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Liveness doesn't check deps
});

// 🏥 Seed Data
using (var scope = app.Services.CreateScope())
{
    var roleRepo = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IRoleRepository>();
    await Cobryx.Infrastructure.Persistence.DbInitializer.SeedRolesAsync(roleRepo);
}

app.Run();

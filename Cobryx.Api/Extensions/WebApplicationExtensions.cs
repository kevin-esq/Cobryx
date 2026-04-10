using Cobryx.Api.Filters;
using Cobryx.Api.Middlewares;
using Cobryx.Infrastructure.Middleware;

using Hangfire;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using Serilog;

namespace Cobryx.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCobryxMiddleware(this WebApplication app, IConfiguration configuration)
    {
        app.UseMetricsAuthentication(configuration);
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.UseSwaggerIfEnabled(configuration);

        app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestIdMiddleware>();
        app.UseMiddleware<DeprecationHeaderMiddleware>();
        app.UseMiddleware<RequestLogContextMiddleware>();
        app.UseMiddleware<DynamicRateLimitingMiddleware>();
        app.UseRateLimiter();
        app.UseCors("DefaultCors");

        app.UseSecurityHeaders();
        app.UseCookiePolicy();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<TenantMiddleware>();
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseMiddleware<SubscriptionGateMiddleware>();
        }

        return app;
    }

    public static WebApplication UseCobryxEndpoints(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "Cobryx Jobs Manager",
            Authorization = [new HangfireDashboardFilter()]
        });

        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }

    private static void UseMetricsAuthentication(this WebApplication app, IConfiguration configuration)
    {
        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/metrics"))
            {
                var metricsKey = configuration["METRICS_SECRET_KEY"];
                if (string.IsNullOrEmpty(metricsKey) || context.Request.Headers["X-Metrics-Key"] != metricsKey)
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
            }
            return next();
        });
    }

    private static void UseSwaggerIfEnabled(this WebApplication app, IConfiguration configuration)
    {
        var enableSwagger = app.Environment.IsDevelopment() || configuration.GetValue<bool>("ENABLE_SWAGGER");

        if (enableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }

    private static void UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; object-src 'none';");
            await next();
        });
    }
}

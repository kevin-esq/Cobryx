using System.Net;
using System.Text.Json;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Cobryx.Domain.Entities;
using Cobryx.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;
    private readonly AppOptions _appOptions;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env, IOptions<AppOptions> appOptions)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _appOptions = appOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Cobryx Critical Error: {Message}", exception.Message);

        try
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
            var userProvider = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>();

            var errorLog = new SystemErrorLog(
                tenantProvider.GetTenantId() ?? Guid.Empty,
                userProvider.GetUserId(),
                exception.Message,
                exception.StackTrace,
                exception.Source,
                context.Request.Path,
                context.Request.Method,
                context.Connection.RemoteIpAddress?.ToString());

            dbContext.Set<SystemErrorLog>().Add(errorLog);
            await dbContext.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception panicEx)
        {
            _logger.LogCritical(panicEx, "FATAL: Could not persist SystemErrorLog to Database.");
        }

        context.Response.ContentType = "application/problem+json";

        var (status, title, type) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, "Domain Constraint Violated", $"{_appOptions.BaseUrl}/errors/domain-error"),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Data Conflict Detected", $"{_appOptions.BaseUrl}/errors/concurrency-error"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", $"{_appOptions.BaseUrl}/errors/internal-server-error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Type = type,
            Detail = _env.IsDevelopment() ? exception.Message : "An internal server error occurred. Reference: " + DateTime.UtcNow.Ticks,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = (int)status;

        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, cancellationToken: context.RequestAborted);
    }
}

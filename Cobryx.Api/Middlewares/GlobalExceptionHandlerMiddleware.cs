using System.Net;
using System.Text.Json;
using Cobryx.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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

        context.Response.ContentType = "application/problem+json";
        
        var (status, title, type) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, "Domain Constraint Violated", "https://cobryx.com.mx/errors/domain-error"),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Data Conflict Detected", "https://cobryx.com.mx/errors/concurrency-error"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", "https://cobryx.com.mx/errors/internal-server-error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Type = type,
            Detail = _env.IsDevelopment() ? exception.Message : "An internal server error occurred. Please contact support.",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = (int)status;

        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails);
    }
}

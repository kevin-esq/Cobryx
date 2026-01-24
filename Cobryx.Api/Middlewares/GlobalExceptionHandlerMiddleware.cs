using System.Net;
using System.Text.Json;
using Cobryx.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cobryx.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        _logger.LogError(exception, "An unexpected error occurred.");

        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            DomainException domainEx => new { error = domainEx.Message, type = "DomainError" },
            _ => new { error = "An internal server error occurred.", type = "ServerError" }
        };

        context.Response.StatusCode = exception switch
        {
            DomainException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, response);
    }
}

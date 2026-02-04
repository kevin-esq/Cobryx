using Cobryx.Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Cobryx.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            Cobryx.Domain.Common.DomainException => (HttpStatusCode.BadRequest, exception.Message),
            Cobryx.Domain.Common.TooManyRequestsException => (HttpStatusCode.TooManyRequests, exception.Message),
            Cobryx.Domain.Common.EmailUnverifiedException => (HttpStatusCode.Forbidden, "Email not verified"),
            FluentValidation.ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }

        object? data = null;
        if (exception is Cobryx.Domain.Common.EmailUnverifiedException)
        {
            data = new
            {
                action = "VERIFY_EMAIL",
                canResend = true
            };
        }

        var traceId = httpContext.TraceIdentifier;
        var response = ApiResponse<object>.FailureResponse(
            message,
            statusCode == HttpStatusCode.Forbidden
                ? new[] { "You must verify your email before logging in." }
                : new[] { exception.Message },
            traceId
        );
        response.Data = data;

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}

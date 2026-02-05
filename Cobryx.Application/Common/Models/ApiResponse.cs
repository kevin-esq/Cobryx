using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cobryx.Application.Common.Models;

/// <summary>
/// Base model for all API responses.
/// </summary>
public abstract class ApiResponse
{
    /// <summary>
    /// Indicates if the request was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; protected set; }

    /// <summary>
    /// Response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Correlation ID for tracing requests.
    /// </summary>
    /// <example>00-123456789-abcd</example>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }
}

/// <summary>
/// Specialized response for successful requests (non-generic for easy attribute usage).
/// </summary>
public class ApiSuccessResponse : ApiResponse
{
    public ApiSuccessResponse() => Success = true;

    /// <summary>
    /// Main response data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Semantic outcome code for successful responses.
    /// </summary>
    /// <example>AUTH.SIGNUP.VERIFICATION_REQUIRED</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outcomeCode")]
    public string? OutcomeCode { get; set; }
}

/// <summary>
/// Specialized response for successful requests (generic for typesafety).
/// </summary>
public class ApiSuccessResponse<T> : ApiResponse
{
    public ApiSuccessResponse() => Success = true;

    /// <summary>
    /// Main response data.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Semantic outcome code for successful responses.
    /// </summary>
    /// <example>AUTH.SIGNUP.VERIFICATION_REQUIRED</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outcomeCode")]
    public string? OutcomeCode { get; set; }
}

/// <summary>
/// Specialized response for failed requests.
/// </summary>
public class ApiErrorResponse : ApiResponse
{
    public ApiErrorResponse() => Success = false;

    /// <summary>
    /// Programmatic error code (string-based).
    /// </summary>
    /// <example>AUTH.INVALID_CREDENTIALS</example>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Programmatic numeric error code.
    /// </summary>
    /// <example>1101</example>
    [JsonPropertyName("numericCode")]
    public int? NumericCode { get; set; }

    /// <summary>
    /// Detailed list of errors (e.g., validation errors).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

/// <summary>
/// Static helpers for response creation.
/// </summary>
public static class ApiResponseFactory
{
    public static ApiSuccessResponse<T> Success<T>(T data, string? outcomeCode = null, string? message = null)
    {
        return new ApiSuccessResponse<T>
        {
            Data = data,
            Message = message,
            OutcomeCode = outcomeCode
        };
    }

    public static ApiSuccessResponse<object?> Success(string? outcomeCode = null, string? message = null)
    {
        return new ApiSuccessResponse<object?>
        {
            Data = null,
            Message = message,
            OutcomeCode = outcomeCode
        };
    }

    public static ApiErrorResponse Error(string message, string? errorCode = null, int? numericCode = null, object? errors = null, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Message = message,
            ErrorCode = errorCode,
            NumericCode = numericCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}

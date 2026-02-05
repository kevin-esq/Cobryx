using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cobryx.Application.Common.Models;

/// <summary>
/// Base model for all API responses.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful.
    /// </summary>
    public virtual bool Success { get; set; }

    /// <summary>
    /// Response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Main response data.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// List of errors if the request failed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Errors { get; set; }

    /// <summary>
    /// Correlation ID for tracing requests.
    /// </summary>
    /// <example>00-123456789-abcd</example>
    public string? TraceId { get; set; }

    /// <summary>
    /// Semantic outcome code for successful responses.
    /// </summary>
    /// <example>AUTH.SIGNUP.VERIFICATION_REQUIRED</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success", string? code = null)
    {
        return new ApiSuccessResponse<T>
        {
            Message = message,
            Data = data,
            Code = code
        };
    }

    public static ApiResponse<T> FailureResponse(string message, IEnumerable<string>? errors = null, string? traceId = null)
    {
        return new ApiErrorResponse<T>
        {
            Message = message,
            Errors = errors,
            TraceId = traceId
        };
    }
}

/// <summary>
/// Specialized response for successful requests.
/// </summary>
public class ApiSuccessResponse<T> : ApiResponse<T>
{
    /// <summary>
    /// Always true for successful responses.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public override bool Success { get; set; } = true;
}

/// <summary>
/// Specialized response for failed requests.
/// </summary>
public class ApiErrorResponse<T> : ApiResponse<T>
{
    /// <summary>
    /// Always false for error responses.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public override bool Success { get; set; } = false;
}

/// <summary>
/// Non-generic version of the API response.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse(string message = "Success", string? code = null)
    {
        return new ApiSuccessResponse
        {
            Message = message,
            Data = null,
            Code = code
        };
    }

    public static new ApiResponse FailureResponse(string message, IEnumerable<string>? errors = null, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Message = message,
            Errors = errors,
            TraceId = traceId
        };
    }
}

/// <summary>
/// Non-generic specialized response for success.
/// </summary>
public class ApiSuccessResponse : ApiResponse
{
    /// <example>true</example>
    [DefaultValue(true)]
    public override bool Success { get; set; } = true;
}

/// <summary>
/// Non-generic specialized response for errors.
/// </summary>
public class ApiErrorResponse : ApiResponse
{
    /// <example>false</example>
    [DefaultValue(false)]
    public override bool Success { get; set; } = false;
}

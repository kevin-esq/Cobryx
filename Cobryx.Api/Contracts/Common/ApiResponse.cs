using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// Professional API Response Envelope.
/// </summary>
public abstract class ApiResponse
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; internal set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outcomeCode")]
    public string? OutcomeCode { get; set; }
}

/// <summary>
/// Successful API response with optional data.
/// </summary>
public class ApiSuccessResponse : ApiResponse
{
    public ApiSuccessResponse() => Success = true;

    /// <summary>
    /// Indicates that the operation was successful.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    [JsonPropertyName("success")]
    public new bool Success { get => base.Success; internal set => base.Success = value; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Strongly typed successful API response.
/// </summary>
public class ApiSuccessResponse<T> : ApiResponse
{
    public ApiSuccessResponse() => Success = true;

    /// <summary>
    /// Indicates that the operation was successful.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    [JsonPropertyName("success")]
    public new bool Success { get => base.Success; internal set => base.Success = value; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Structured validation error for field-level API error reporting.
/// </summary>
/// <param name="Field">The request field that caused the error (e.g., "amount", "customerId").</param>
/// <param name="Code">A machine-readable error code (e.g., "AMOUNT_NEGATIVE", "REQUIRED").</param>
/// <param name="Message">A human-readable explanation of the error.</param>
public record ValidationError(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message
);

/// <summary>
/// Error API response with machine-readable codes and structured field-level errors.
/// </summary>
public class ApiErrorResponse : ApiResponse
{
    public ApiErrorResponse()
    {
        Success = false;
    }

    /// <summary>
    /// Indicates that the operation has failed.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    [JsonPropertyName("success")]
    public new bool Success { get => base.Success; internal set => base.Success = value; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("numericCode")]
    public int? NumericCode { get; set; }

    /// <summary>
    /// Structured field-level validation errors. Each entry identifies a specific field, error code, and message.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public IReadOnlyList<ValidationError>? Errors { get; set; }
}

/// <summary>
/// Static factory for creating API responses.
/// </summary>
public static class ApiResponseFactory
{
    public static ApiSuccessResponse<T> Success<T>(T data, string? outcomeCode = null)
    {
        return new ApiSuccessResponse<T>
        {
            Data = data,
            OutcomeCode = outcomeCode
        };
    }

    public static ApiSuccessResponse Success(string? outcomeCode = null)
    {
        return new ApiSuccessResponse
        {
            Data = null,
            OutcomeCode = outcomeCode
        };
    }

    public static ApiErrorResponse Error(
        string? errorCode = null,
        int? numericCode = null,
        IReadOnlyList<ValidationError>? errors = null,
        string? traceId = null,
        string? outcomeCode = null)
    {
        var response = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            OutcomeCode = outcomeCode,
            NumericCode = numericCode,
            Errors = errors,
            TraceId = traceId
        };

        response.Success = false; // Guaranteed explicit override
        return response;
    }
}

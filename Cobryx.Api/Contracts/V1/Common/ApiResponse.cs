using System.ComponentModel;
using System.Text.Json.Serialization;

using Cobryx.Domain.Shared;

namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// Professional API Response Envelope.
/// </summary>
public abstract class ApiResponse
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    /// <example>true</example>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>A unique correlation ID for the request.</summary>
    /// <example>0HN72V0R8M5E1:00000001</example>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>A machine-readable code describing the outcome.</summary>
    /// <example>AUTH.SIGNUP.SUCCESS</example>
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

    /// <summary>The payload data (null for simple acknowledgments).</summary>
    /// <example>null</example>
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

    /// <summary>The strongly-typed payload data.</summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Structured validation error for field-level API error reporting.
/// The frontend translates codes — messages are internal-only (logs/debugging).
/// </summary>
public record ValidationError(string Field, string Code, string? Message = null)
{
    /// <summary>The request field that caused the error.</summary>
    /// <example>email</example>
    [JsonPropertyName("field")]
    public string Field { get; init; } = Field;

    /// <summary>A machine-readable error code.</summary>
    /// <example>VALIDATION.CUSTOMER.EMAIL.INVALID_FORMAT</example>
    [JsonPropertyName("code")]
    public string Code { get; init; } = Code;

    /// <summary>Internal-only message for logging and debugging. Never serialized to the client.</summary>
    [JsonIgnore]
    public string? Message { get; init; } = Message;
}

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

    /// <summary>High-level error code.</summary>
    /// <example>VALIDATION_ERROR</example>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>Standard HTTP status code or internal numeric code.</summary>
    /// <example>400</example>
    [JsonPropertyName("numericCode")]
    public int? NumericCode { get; set; }

    /// <summary>
    /// Structured field-level validation errors.
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
    public static ApiSuccessResponse<T> Success<T>(T data, Outcome? outcomeCode = null)
    {
        return new ApiSuccessResponse<T>
        {
            Data = data,
            OutcomeCode = outcomeCode?.Value
        };
    }

    public static ApiSuccessResponse Success(Outcome? outcomeCode = null)
    {
        return new ApiSuccessResponse
        {
            Data = null,
            OutcomeCode = outcomeCode?.Value
        };
    }

    public static ApiErrorResponse Error(
        string? errorCode = null,
        int? numericCode = null,
        IReadOnlyList<ValidationError>? errors = null,
        string? traceId = null,
        Outcome? outcomeCode = null)
    {
        return new ApiErrorResponse
        {
            ErrorCode = errorCode,
            OutcomeCode = outcomeCode?.Value,
            NumericCode = numericCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}

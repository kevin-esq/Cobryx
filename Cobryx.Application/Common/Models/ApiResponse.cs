using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cobryx.Application.Common.Models;

public abstract class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; protected set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }
}

public class ApiSuccessResponse : ApiResponse
{
    public ApiSuccessResponse() => Success = true;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outcomeCode")]
    public string? OutcomeCode { get; set; }
}

public class ApiSuccessResponse<T> : ApiResponse
{
    public ApiSuccessResponse() => Success = true;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("outcomeCode")]
    public string? OutcomeCode { get; set; }
}

public class ApiErrorResponse : ApiResponse
{
    public ApiErrorResponse() => Success = false;

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("numericCode")]
    public int? NumericCode { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

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

    public static ApiSuccessResponse<object?> Success(string? outcomeCode = null)
    {
        return new ApiSuccessResponse<object?>
        {
            Data = null,
            OutcomeCode = outcomeCode
        };
    }

    public static ApiErrorResponse Error(string? errorCode = null, int? numericCode = null, object? errors = null, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            ErrorCode = errorCode,
            NumericCode = numericCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}

namespace Cobryx.Api.Filters;

public partial class GlobalExceptionHandler
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred: {message} [Code: {numericCode}]")]
    public static partial void LogUnhandledException(ILogger logger, Exception ex, string message, int numericCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Application exception: {message} [Code: {numericCode}]")]
    public static partial void LogApplicationException(ILogger logger, string message, int numericCode);
}

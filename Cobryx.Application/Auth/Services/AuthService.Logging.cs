using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Services;

public partial class AuthService
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Error generating auth response for user {userId}")]
    public static partial void LogAuthResponseGenerationError(ILogger logger, Exception ex, Guid userId);
}

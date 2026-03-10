namespace Cobryx.Application.Common.Interfaces;

public interface ISlackService
{
    Task SendAlertAsync(string message, string? title = null, string? color = null);
}

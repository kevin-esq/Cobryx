using Cobryx.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ValidateCaptchaAttribute : Attribute, IAsyncActionFilter
{
    private const string CaptchaHeaderName = "X-Captcha-Token";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var captchaService = context.HttpContext.RequestServices.GetRequiredService<ICaptchaService>();
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        string? captchaToken = context.HttpContext.Request.Headers[CaptchaHeaderName];

        if (string.IsNullOrEmpty(captchaToken))
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                var captchaProp = argument?.GetType().GetProperty("CaptchaToken");
                if (captchaProp != null)
                {
                    captchaToken = captchaProp.GetValue(argument) as string;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(captchaToken))
        {
            context.Result = new BadRequestObjectResult(new { Error = "CAPTCHA token is missing." });
            return;
        }

        var isValid = await captchaService.VerifyAsync(captchaToken, ipAddress);
        if (!isValid)
        {
            context.Result = new BadRequestObjectResult(new { Error = "CAPTCHA verification failed." });
            return;
        }

        await next();
    }
}

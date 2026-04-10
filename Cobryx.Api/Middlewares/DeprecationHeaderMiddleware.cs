using Cobryx.Api.Attributes;

namespace Cobryx.Api.Middlewares;

/// <summary>
/// Injects standard Deprecation and Sunset headers when an action is decorated with [Deprecated].
/// </summary>
public class DeprecationHeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var endpoint = context.GetEndpoint();
            var attribute = endpoint?.Metadata.GetMetadata<DeprecatedAttribute>();

            if (attribute != null)
            {
                context.Response.Headers.Append("Deprecation", "true");

                if (!string.IsNullOrEmpty(attribute.SunsetDate))
                {
                    context.Response.Headers.Append("Sunset", attribute.SunsetDate);
                }

                if (!string.IsNullOrEmpty(attribute.AlternativeUri))
                {
                    context.Response.Headers.Append("Link", $"<{attribute.AlternativeUri}>; rel=\"deprecation\"");
                }
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

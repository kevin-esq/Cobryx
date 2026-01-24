using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // TODO: Future: Register MediatR, AutoMapper, Validators here
        return services;
    }
}

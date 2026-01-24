using System.Reflection;
using Cobryx.Application.Common.Behaviors;
using Concordia;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register Concordia Core Services
        services.AddConcordiaCoreServices();
        
        // The Source Generator creates this method
        services.AddConcordiaHandlers();

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

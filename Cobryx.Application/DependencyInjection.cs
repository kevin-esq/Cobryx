using Cobryx.Domain.DomainServices;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cobryx.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register Concordia Core Services
        services.AddConcordiaCoreServices();
        services.AddConcordiaHandlers();

        // Domain Services
        services.AddScoped<IScheduleGenerator, ScheduleGenerator>();

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

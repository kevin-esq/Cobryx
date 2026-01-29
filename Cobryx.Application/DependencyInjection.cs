using Cobryx.Domain.DomainServices;
using Cobryx.Domain.Interfaces;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Services;
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

        services.AddConcordiaCoreServices();
        services.AddConcordiaHandlers();

        services.AddScoped<IScheduleGenerator, ScheduleGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

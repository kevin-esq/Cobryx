using System.Reflection;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Credits.Interfaces;
using Cobryx.Application.Credits.Services;
using Cobryx.Application.Auth.Interfaces;
using Cobryx.Application.Auth.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<Payments.Services.PaymentLinkReconciliationService>();
        services.AddScoped<Accounting.Services.FinancialPostingEngine>();

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

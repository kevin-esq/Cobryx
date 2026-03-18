using System.Reflection;

using Cobryx.Application.Auth.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Modules;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Standard Concordia (Mediator) & Validation
        // Explicit call to Source Generated registrations to assist IDE/OmniSharp resolution
        ConcordiaGeneratedRegistrations.AddConcordiaHandlers(services);
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<INotificationPublisher, ForeachAwaitPublisher>();

        services.AddValidatorsFromAssembly(assembly);

        // Core Business Services
        services.AddScoped<IAuthService, AuthService>();

        // Fintech & Payments Engine
        services.AddLendingModule();
        services.AddPaymentsModule();
        services.AddAccountingModule();

        services.AddScoped<Analytics.Services.IPortfolioAnalyticsService, Analytics.Services.PortfolioAnalyticsService>();

        return services;
    }
}

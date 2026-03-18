using System.Reflection;

using Cobryx.Application.Auth.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Modules;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Cobryx.Application.Risk;
using Cobryx.Application.Risk.Factors;

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

        // Risk Engine
        services.AddScoped<ProbabilityOfDefaultCalculator>(_ =>
            new ProbabilityOfDefaultCalculator(new (IRiskFactor Factor, decimal Weight)[]
            {
                (new DpdRiskFactor(), 0.4m),
                (new UtilizationRiskFactor(), 0.2m),
                (new PaymentDelayRiskFactor(), 0.2m),
                (new TrendRiskFactor(), 0.2m)
            }));
            
        services.AddScoped<Cobryx.Application.Risk.Jobs.EarlyWarningJob>();

        services.AddValidatorsFromAssembly(assembly);

        // Core Business Services
        services.AddScoped<IAuthService, AuthService>();

        // Fintech & Payments Engine
        services.AddLendingModule();
        services.AddPaymentsModule();
        services.AddAccountingModule();

        services.AddScoped<Analytics.Services.IPortfolioAnalyticsService, Analytics.Services.PortfolioAnalyticsService>();
        services.AddScoped<Collections.Strategy.ICollectionsStrategyEngine, Collections.Strategy.CollectionsStrategyEngine>();

        return services;
    }
}

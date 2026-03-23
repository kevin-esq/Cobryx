using System.Reflection;

using Polly;

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
        services.AddScoped<Domain.Decision.ICreditLimitEngine, Decision.CreditLimitEngine>();
        services.AddScoped<Domain.Decision.IPricingEngine, Decision.PricingEngine>();
        services.AddScoped<Domain.Decision.IFraudEngine, Decision.FraudEngine>();
        services.AddScoped<Decision.DecisionEngine>();
        services.AddScoped<Decision.DecisionService>();
        services.AddScoped<Decision.IRiskEvaluator, Decision.DefaultRiskEvaluator>();
        services.AddScoped<ML.GuardrailEngine>();
        services.AddScoped<ML.IFeatureStore, ML.RedisFeatureStore>();
        services.AddScoped<ML.FeatureUpdater>();
        services.AddScoped<ML.DatasetExporter>();
        services.AddScoped<ML.Jobs.DatasetExporterJob>();
        services.AddScoped<ML.Jobs.RlTrainingJob>();
        services.AddScoped<ML.Jobs.PortfolioTrainingJob>();
        services.AddScoped<ML.Jobs.MacroIngestionJob>();
        services.AddScoped<ML.ModelRouter>();
        services.AddScoped<ML.EnsembleService>();
        services.AddScoped<ML.RlPolicy>();
        services.AddScoped<ML.IRlEngine, ML.RlEngine>();
        services.AddScoped<ML.ScenarioGenerator>();
        services.AddScoped<ML.MonteCarloEvaluator>();
        services.AddScoped<ML.IPortfolioFeatureStore, ML.RedisPortfolioFeatureStore>();

        services.AddHttpClient<ML.MonteCarloPpoClient>(c =>
            {
                var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                c.BaseAddress = new Uri(url);
            })
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, retry =>
                    TimeSpan.FromMilliseconds(200 * retry)))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
        services.AddScoped<ML.PortfolioEngine>();

        services.AddHttpClient<ML.MlClient>(c =>
        {
            var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
            c.BaseAddress = new Uri(url);
        });

        services.AddHttpClient<ML.PpoClient>(c =>
            {
                var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                c.BaseAddress = new Uri(url);
            })
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, retry =>
                    TimeSpan.FromMilliseconds(200 * retry)))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddHttpClient<ML.PortfolioPpoClient>(c =>
            {
                var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                c.BaseAddress = new Uri(url);
            })
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, retry =>
                    TimeSpan.FromMilliseconds(200 * retry)))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddScoped<ProbabilityOfDefaultCalculator>(_ =>
            new ProbabilityOfDefaultCalculator(new (IRiskFactor Factor, decimal Weight)[]
            {
                (new DpdRiskFactor(), 0.4m),
                (new UtilizationRiskFactor(), 0.2m),
                (new PaymentDelayRiskFactor(), 0.2m),
                (new TrendRiskFactor(), 0.2m)
            }));

        services.AddScoped<Risk.Jobs.EarlyWarningJob>();

        services.AddValidatorsFromAssembly(assembly);

        // Core Business Services
        services.AddScoped<IAuthService, AuthService>();

        // Fintech & Payments Engine
        services.AddLendingModule();
        services.AddPaymentsModule();
        services.AddAccountingModule();

        services
            .AddScoped<Analytics.Services.IPortfolioAnalyticsService, Analytics.Services.PortfolioAnalyticsService>();
        services
            .AddScoped<Collections.Strategy.ICollectionsStrategyEngine,
                Collections.Strategy.CollectionsStrategyEngine>();

        return services;
    }
}

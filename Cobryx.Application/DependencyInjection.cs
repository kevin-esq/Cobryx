using System.Reflection;

using Cobryx.Application.Auth.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Modules;
using Cobryx.Application.Risk;
using Cobryx.Application.Risk.Factors;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Polly;

namespace Cobryx.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            _ = services.AddConcordiaHandlers();
            _ = services.AddScoped<IMediator, Mediator>();
            _ = services.AddScoped<ISender>(static sp => sp.GetRequiredService<IMediator>());
            _ = services.AddScoped<INotificationPublisher, DefaultNotificationPublisher>();

            _ = services.AddScoped<Domain.Decision.ICreditLimitEngine, Decision.CreditLimitEngine>();
            _ = services.AddScoped<Domain.Decision.IPricingEngine, Decision.PricingEngine>();
            _ = services.AddScoped<Domain.Decision.IFraudEngine, Decision.FraudEngine>();
            _ = services.AddScoped<Decision.DecisionEngine>();
            _ = services.AddScoped<Decision.DecisionService>();
            _ = services.AddScoped<Decision.IRiskEvaluator, Decision.DefaultRiskEvaluator>();
            _ = services.AddScoped<ML.IMacroFeatureStore, ML.RedisMacroFeatureStore>();
            _ = services.AddSingleton<Decision.SnapshotStore>();
            _ = services.AddSingleton<Decision.ISnapshotStore>(static sp =>
                sp.GetRequiredService<Decision.SnapshotStore>());
            services.AddScoped<Decision.Interfaces.IReplayEngine, ML.ReplayEngine>();
            services.AddScoped<ML.ReplayEngine>();
            _ = services.AddScoped<Decision.SnapshotRegressionRunner>();
            _ = services.AddScoped<Decision.Interfaces.IShadowComparer, Decision.ShadowComparer>();

            _ = services.AddScoped<ML.GuardrailEngine>();
            _ = services.AddScoped<ML.IFeatureStore, ML.RedisFeatureStore>();
            _ = services.AddScoped<ML.FeatureUpdater>();
            _ = services.AddScoped<ML.DatasetExporter>();
            _ = services.AddScoped<ML.Jobs.DatasetExporterJob>();
            _ = services.AddScoped<ML.Jobs.RlTrainingJob>();
            _ = services.AddScoped<ML.Jobs.PortfolioTrainingJob>();
            _ = services.AddScoped<ML.Jobs.MacroIngestionJob>();
            _ = services.AddScoped<ML.ModelRouter>();
            _ = services.AddScoped<ML.EnsembleService>();
            _ = services.AddScoped<ML.RlPolicy>();
            _ = services.AddScoped<ML.IRlEngine, ML.RlEngine>();
            _ = services.AddScoped<ML.ScenarioGenerator>();
            _ = services.AddScoped<ML.MonteCarloEvaluator>();
            _ = services.AddScoped<ML.IPortfolioFeatureStore, ML.RedisPortfolioFeatureStore>();

            _ = services.AddHttpClient<ML.MonteCarloPpoClient>(static c =>
                {
                    var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                    c.BaseAddress = new Uri(url);
                })
                .AddTransientHttpErrorPolicy(static p =>
                    p.WaitAndRetryAsync(3, static retry =>
                        TimeSpan.FromMilliseconds(200 * retry)))
                .AddTransientHttpErrorPolicy(static p =>
                    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
            _ = services.AddScoped<ML.IPortfolioEngine, ML.PortfolioEngine>();
            _ = services.AddScoped<ML.IMonteCarloEvaluator, ML.MonteCarloEvaluator>();

            _ = services.AddHttpClient<ML.IMlClient, ML.MlClient>(static c =>
            {
                var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                c.BaseAddress = new Uri(url);
            });

            _ = services.AddHttpClient<ML.MonteCarloPpoClient>(static c =>
            {
                var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                c.BaseAddress = new Uri(url);
            });


            _ = services.AddHttpClient<ML.PpoClient>(static c =>
                {
                    var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                    c.BaseAddress = new Uri(url);
                })
                .AddTransientHttpErrorPolicy(static p =>
                    p.WaitAndRetryAsync(3, static retry =>
                        TimeSpan.FromMilliseconds(200 * retry)))
                .AddTransientHttpErrorPolicy(static p =>
                    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

            _ = services.AddHttpClient<ML.PortfolioPpoClient>(static c =>
                {
                    var url = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";
                    c.BaseAddress = new Uri(url);
                })
                .AddTransientHttpErrorPolicy(static p =>
                    p.WaitAndRetryAsync(3, static retry =>
                        TimeSpan.FromMilliseconds(200 * retry)))
                .AddTransientHttpErrorPolicy(static p =>
                    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

            _ = services.AddScoped(static _ =>
                new ProbabilityOfDefaultCalculator(
                [
                    (new DpdRiskFactor(), 0.4m),
                    (new UtilizationRiskFactor(), 0.2m),
                    (new PaymentDelayRiskFactor(), 0.2m),
                    (new TrendRiskFactor(), 0.2m)
                ]));

            _ = services.AddScoped<Risk.Jobs.EarlyWarningJob>();

            _ = services.AddValidatorsFromAssembly(assembly);

            _ = services.AddScoped<IAuthService, AuthService>();

            _ = services.AddLendingModule();
            _ = services.AddPaymentsModule();
            _ = services.AddAccountingModule();

            _ = services
                .AddScoped<Analytics.Services.IPortfolioAnalyticsService,
                    Analytics.Services.PortfolioAnalyticsService>();
            _ = services
                .AddScoped<Collections.Strategy.ICollectionsStrategyEngine,
                    Collections.Strategy.CollectionsStrategyEngine>();

            return services;
        }

        private class DefaultNotificationPublisher : INotificationPublisher
        {
            public async Task Publish(IEnumerable<Func<INotification, CancellationToken, Task>> handlerCallbacks, INotification notification, CancellationToken cancellationToken)
            {
                foreach (var callback in handlerCallbacks)
                {
                    await callback(notification, cancellationToken);
                }
            }
        }
    }
}

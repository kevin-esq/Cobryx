using System.Reflection;

using Cobryx.Application.Auth.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Modules;
using Cobryx.Application.Risk;
using Cobryx.Application.Risk.Factors;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // NOTE: ML Infrastructure (IFeatureStore, IMlClient, etc.) is registered
            // in Infrastructure.ML.MlInfrastructureModule - Application stays pure
            _ = services
                .AddMediator()
                .AddDecisionServices()
                .AddMlApplicationServices()
                .AddRiskServices()
                .AddValidatorsFromAssembly(assembly)
                .AddScoped<IAuthService, AuthService>()
                .AddLendingModule()
                .AddPaymentsModule()
                .AddAccountingModule()
                .AddAnalyticsServices();

            return services;
        }

        private static IServiceCollection AddMediator(this IServiceCollection services)
        {
            _ = services.AddConcordiaHandlers();
            _ = services.AddScoped<IMediator, Mediator>();
            _ = services.AddScoped<ISender>(static sp => sp.GetRequiredService<IMediator>());
            _ = services.AddScoped<INotificationPublisher, SequentialNotificationPublisher>();

            return services;
        }

        private static IServiceCollection AddDecisionServices(this IServiceCollection services)
        {
            _ = services.AddScoped<Domain.Decision.ICreditLimitEngine, Decision.CreditLimitEngine>();
            _ = services.AddScoped<Domain.Decision.IPricingEngine, Decision.PricingEngine>();
            _ = services.AddScoped<Domain.Decision.IFraudEngine, Decision.FraudEngine>();
            _ = services.AddScoped<Decision.DecisionEngine>();
            _ = services.AddScoped<Decision.DecisionService>();
            _ = services.AddScoped<Decision.IRiskEvaluator, Decision.DefaultRiskEvaluator>();
            _ = services.AddSingleton<Decision.SnapshotStore>();
            _ = services.AddSingleton<Decision.ISnapshotStore>(static sp =>
                sp.GetRequiredService<Decision.SnapshotStore>());
            _ = services.AddScoped<Decision.Interfaces.IReplayEngine, ML.ReplayEngine>();
            _ = services.AddScoped<ML.ReplayEngine>();
            _ = services.AddScoped<Decision.SnapshotRegressionRunner>();
            _ = services.AddScoped<Decision.Interfaces.IShadowComparer, Decision.ShadowComparer>();

            return services;
        }

        /// <summary>
        /// Registers Application-layer ML services (pure logic, no infrastructure).
        /// Infrastructure implementations (IFeatureStore, IMlClient, etc.) are registered
        /// in Infrastructure.ML.MlInfrastructureModule.
        /// </summary>
        private static IServiceCollection AddMlApplicationServices(this IServiceCollection services)
        {
            _ = services.AddScoped<ML.GuardrailEngine>();
            _ = services.AddScoped<ML.FeatureUpdater>();
            _ = services.AddScoped<ML.DatasetExporter>();
            _ = services.AddScoped<ML.Jobs.DatasetExporterJob>();
            _ = services.AddScoped<ML.Jobs.RlTrainingJob>();
            _ = services.AddScoped<ML.Jobs.PortfolioTrainingJob>();
            _ = services.AddScoped<ML.Jobs.MacroIngestionJob>();
            _ = services.AddScoped<ML.EnsembleService>();
            _ = services.AddScoped<ML.RlPolicy>();
            _ = services.AddScoped<ML.IRlEngine, ML.RlEngine>();
            _ = services.AddScoped<ML.ScenarioGenerator>();
            _ = services.AddScoped<ML.MonteCarloEvaluator>();
            _ = services.AddScoped<ML.IPortfolioEngine, ML.PortfolioEngine>();
            _ = services.AddScoped<ML.IMonteCarloEvaluator, ML.MonteCarloEvaluator>();

            return services;
        }

        private static IServiceCollection AddRiskServices(this IServiceCollection services)
        {
            _ = services.AddScoped(static _ =>
                new ProbabilityOfDefaultCalculator(
                [
                    (new DpdRiskFactor(), 0.4m),
                    (new UtilizationRiskFactor(), 0.2m),
                    (new PaymentDelayRiskFactor(), 0.2m),
                    (new TrendRiskFactor(), 0.2m)
                ]));

            _ = services.AddScoped<Risk.Jobs.EarlyWarningJob>();

            return services;
        }

        private static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
        {
            _ = services.AddScoped<Analytics.Services.IPortfolioAnalyticsService,
                Analytics.Services.PortfolioAnalyticsService>();

            _ = services.AddScoped<Collections.Strategy.ICollectionsStrategyEngine,
                Collections.Strategy.CollectionsStrategyEngine>();

            _ = services.AddScoped<Analytics.Snapshots.ILoanSnapshotFactory,
                Analytics.Snapshots.LoanSnapshotFactory>();

            return services;
        }

        private sealed class SequentialNotificationPublisher : INotificationPublisher
        {
            public async Task Publish(
                IEnumerable<Func<INotification, CancellationToken, Task>> handlerCallbacks,
                INotification notification,
                CancellationToken cancellationToken)
            {
                foreach (var callback in handlerCallbacks)
                {
                    await callback(notification, cancellationToken);
                }
            }
        }
    }
}

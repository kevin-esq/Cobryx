using Cobryx.Application.ML.Interfaces;
using Cobryx.Infrastructure.ML.Clients;
using Cobryx.Infrastructure.ML.FeatureStores;
using Cobryx.Infrastructure.ML.Routing;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Extensions.Http;

namespace Cobryx.Infrastructure.ML;

public static class MlInfrastructureModule
{
    private static readonly string MlServiceUrl =
        Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://localhost:8001";

    public static IServiceCollection AddMlInfrastructure(this IServiceCollection services)
    {
        // Feature Stores
        services.AddScoped<IFeatureStore, RedisFeatureStore>();
        services.AddScoped<IMacroFeatureStore, RedisMacroFeatureStore>();
        services.AddScoped<IPortfolioFeatureStore, RedisPortfolioFeatureStore>();

        // Routing
        services.AddScoped<IModelRouter, ModelRouter>();

        // HTTP Clients
        services.AddHttpClient<IMlClient, MlClient>(ConfigureMlHttpClient)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IPpoClient, PpoClient>(ConfigureMlHttpClient)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IPortfolioPpoClient, PortfolioPpoClient>(ConfigureMlHttpClient)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IMonteCarloPpoClient, MonteCarloPpoClient>(ConfigureMlHttpClient)
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static void ConfigureMlHttpClient(HttpClient client)
    {
        client.BaseAddress = new Uri(MlServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}

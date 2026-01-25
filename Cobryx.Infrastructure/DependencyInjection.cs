using Cobryx.Infrastructure.Middleware;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.MultiTenancy;
using Cobryx.Infrastructure.Repositories;
using Concordia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        // Pipeline Behaviors (Registered here to bypass Application's source generator)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Logging<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Validation<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Audit<,>));

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICreditRepository, CreditRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}

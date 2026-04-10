using Cobryx.Application.Payments.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application.Modules;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddScoped<PaymentLinkReconciliationService>();

        return services;
    }
}

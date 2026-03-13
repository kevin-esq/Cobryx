using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Payments.Services;
using Cobryx.Infrastructure.Payments.Stripe;
using Cobryx.Infrastructure.Repositories;
using Cobryx.Infrastructure.Webhooks.Stripe;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Infrastructure.Modules;

public static class PaymentsInfrastructureModule
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();

        // Stripe & Orchestration
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IPaymentOrchestrationService, PaymentOrchestrationService>();
        services.AddScoped<Cobryx.Application.Subscriptions.Services.StripeSubscriptionSyncService>();
        services.AddScoped<IWebhookParser, StripeWebhookParser>();

        return services;
    }
}

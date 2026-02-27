using System.Reflection;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Services;
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
        services.AddConcordiaHandlers();
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddValidatorsFromAssembly(assembly);

        // Core Business Services
        services.AddScoped<IAuthService, AuthService>();

        // Fintech & Payments Engine
        services.AddScoped<Payments.Services.PaymentLinkReconciliationService>();
        services.AddScoped<Accounting.Services.FinancialPostingEngine>();
        services.AddScoped<Accounting.Services.ReconciliationEngine>();
        services.AddScoped<Lending.Services.FinancialStateEngine>();
        services.AddScoped<ILedgerIntegrityService, Accounting.Services.LedgerIntegrityService>();
        services.AddScoped<Accounting.Services.IBankReconciliationEngine, Accounting.Services.BankReconciliationEngine>();

        return services;
    }
}

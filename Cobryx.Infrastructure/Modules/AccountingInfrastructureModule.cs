using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Messaging;
using Cobryx.Infrastructure.Repositories;
using Cobryx.Infrastructure.Services.Accounting;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Infrastructure.Modules;

public static class AccountingInfrastructureModule
{
    public static IServiceCollection AddAccountingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddScoped<ILedgerBalanceService, LedgerBalanceService>();
        services.AddScoped<IFinancialEventBus, LocalFinancialEventBus>();
        services.AddScoped<IFinancialEventConsumer, EventShadowReplayEngine>();
        services.AddScoped<ILedgerPublisher, LogLedgerPublisher>();
        services.AddScoped<IShadowReplayEngine, ShadowReplayEngine>();

        return services;
    }
}

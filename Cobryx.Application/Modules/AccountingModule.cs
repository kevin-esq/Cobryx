using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application.Modules;

public static class AccountingModule
{
    public static IServiceCollection AddAccountingModule(this IServiceCollection services)
    {
        services.AddScoped<FinancialPostingEngine>();
        services.AddScoped<ReconciliationEngine>();
        services.AddScoped<ILedgerIntegrityService, LedgerIntegrityService>();
        services.AddScoped<IBankReconciliationEngine, BankReconciliationEngine>();

        return services;
    }
}

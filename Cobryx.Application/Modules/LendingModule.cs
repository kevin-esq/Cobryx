using Cobryx.Application.Lending.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Application.Modules;

public static class LendingModule
{
    public static IServiceCollection AddLendingModule(this IServiceCollection services)
    {
        services.AddScoped<FinancialStateEngine>();
        services.AddScoped<LoanPaymentService>();

        return services;
    }
}

using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Infrastructure.Lending;
using Cobryx.Infrastructure.Repositories.Lending;

using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Infrastructure.Modules;

public static class LendingInfrastructureModule
{
    public static IServiceCollection AddLendingInfrastructure(this IServiceCollection services)
    {
        // Domain Repositories
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanAgreementRepository, LoanAgreementRepository>();
        services.AddScoped<IInstallmentRepository, InstallmentRepository>();
        services.AddScoped<ICreditSaleRepository, CreditSaleRepository>();

        services.AddScoped<IInterestPolicyRepository, PolicyRepository>();
        services.AddScoped<ILateFeePolicyRepository, PolicyRepository>();
        services.AddScoped<IPaymentApplicationPolicyRepository, PolicyRepository>();

        services.AddScoped<IAmortizationService, AmortizationService>();
        services.AddScoped<IScheduleGenerator, ScheduleGenerator>();
        services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();
        services.AddScoped<ILoanAccrualEngine, LoanAccrualEngine>();
        services.AddScoped<ILateFeeService, LateFeeService>();
        services.AddScoped<IPaymentAllocationEngine, PaymentAllocationEngine>();
        services.AddScoped<ICollectionsEngine, CollectionsEngine>();

        return services;
    }
}

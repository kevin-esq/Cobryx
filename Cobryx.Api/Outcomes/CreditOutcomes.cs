using Cobryx.Domain.Common;

namespace Cobryx.Api.Outcomes;

public static class CreditOutcomes
{
    private const string Prefix = "CREDIT";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "Credit created successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Credit search completed.");
    public static readonly Outcome PaymentRegistered = new($"{Prefix}.PAYMENT_SUCCESS", OutcomeCategory.Success, "Credit payment registered.");
    public static readonly Outcome InstallmentsGenerated = new($"{Prefix}.INSTALLMENTS_SUCCESS", OutcomeCategory.Success, "Credit installments generated.");
    public static readonly Outcome LateFeesApplied = new($"{Prefix}.LATE_FEES_SUCCESS", OutcomeCategory.Success, "Late fees applied to credit.");

    public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Credit validation failed.");
    public static readonly Outcome Conflict = new($"{Prefix}.CONFLICT_ERROR", OutcomeCategory.BusinessError, "Credit conflict occurred.");
}

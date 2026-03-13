using Cobryx.Application.Common.Validation;

using FluentValidation;

namespace Cobryx.Application.Credits.Commands.Create;

public class CreateCreditValidator : AbstractValidator<CreateCreditCommand>
{
    public CreateCreditValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode("VALIDATION.CREDIT.TENANT_ID.REQUIRED");
        RuleFor(x => x.CustomerId).NotEmpty().WithErrorCode("VALIDATION.CREDIT.CUSTOMER_ID.REQUIRED");
        RuleFor(x => x.Amount).GreaterThan(0).WithErrorCode(CreditValidationErrors.Amount.MustBePositive);
        RuleFor(x => x.Currency).NotEmpty().WithErrorCode("VALIDATION.CREDIT.CURRENCY.REQUIRED")
            .Length(3).WithErrorCode("VALIDATION.CREDIT.CURRENCY.INVALID_LENGTH");
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0).WithErrorCode(CreditValidationErrors.InterestRate.NegativeForbidden);
        RuleFor(x => x.InstallmentsCount).GreaterThan(0).WithErrorCode(CreditValidationErrors.Installments.MustBePositive);
        RuleFor(x => x.Frequency).IsInEnum().WithErrorCode("VALIDATION.CREDIT.FREQUENCY.INVALID");
    }
}

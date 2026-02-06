using FluentValidation;
using Cobryx.Application.Common.Validation;

namespace Cobryx.Application.Payments.Commands.Register;

public class RegisterPaymentValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode("VALIDATION.PAYMENT.TENANT_ID.REQUIRED");
        RuleFor(x => x.CreditId).NotEmpty().WithErrorCode("VALIDATION.PAYMENT.CREDIT_ID.REQUIRED");
        RuleFor(x => x.Amount).GreaterThan(0).WithErrorCode(PaymentValidationErrors.Amount.MustBePositive);
        RuleFor(x => x.Currency).NotEmpty().WithErrorCode(PaymentValidationErrors.Currency.Required)
            .Length(3).WithErrorCode(PaymentValidationErrors.Currency.InvalidLength);
        RuleFor(x => x.PaymentDate).NotEmpty().WithErrorCode(PaymentValidationErrors.Date.Required);
    }
}

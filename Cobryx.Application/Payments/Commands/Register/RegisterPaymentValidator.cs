using FluentValidation;

namespace Cobryx.Application.Payments.Commands.Register;

public class RegisterPaymentValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CreditId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PaymentDate).NotEmpty();
    }
}

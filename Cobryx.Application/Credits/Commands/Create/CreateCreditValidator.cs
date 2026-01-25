using FluentValidation;
using Cobryx.Domain.Enums;

namespace Cobryx.Application.Credits.Commands.Create;

public class CreateCreditValidator : AbstractValidator<CreateCreditCommand>
{
    public CreateCreditValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InstallmentsCount).GreaterThan(0);
        RuleFor(x => x.Frequency).IsInEnum();
    }
}

using FluentValidation;

namespace Cobryx.Application.Products.Commands.Create;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BasePrice).NotNull();
        
        When(x => x.IsLoanProduct, () => {
            RuleFor(x => x.DefaultInterestRate).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxInstallments).GreaterThan(0);
        });
    }
}

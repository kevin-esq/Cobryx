using Cobryx.Application.Common.Validation;

using FluentValidation;

namespace Cobryx.Application.Products.Commands.Create;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode("VALIDATION.PRODUCT.TENANT_ID.REQUIRED");
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(ProductValidationErrors.Name.Required)
            .MaximumLength(200).WithErrorCode(ProductValidationErrors.Name.TooLong);
        RuleFor(x => x.BasePrice).NotNull().WithErrorCode(ProductValidationErrors.BasePrice.Required);

        When(x => x.IsLoanProduct, () =>
        {
            RuleFor(x => x.DefaultInterestRate)
                .GreaterThanOrEqualTo(0).WithErrorCode(ProductValidationErrors.InterestRate.NegativeForbidden);
            RuleFor(x => x.MaxInstallments)
                .GreaterThan(0).WithErrorCode(ProductValidationErrors.MaxInstallments.MustBePositive);
        });
    }
}

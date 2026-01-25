using FluentValidation;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+?[1-9]\d{1,14}$") // Basic E.164-ish regex
            .WithMessage("Invalid phone format.");
        
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

using FluentValidation;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(v => v.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(v => v.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Invalid phone format.");

        RuleFor(x => x.Address!)
            .SetValidator(new AddressValidator())
            .When(x => x.Address != null);
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExtNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
    }
}

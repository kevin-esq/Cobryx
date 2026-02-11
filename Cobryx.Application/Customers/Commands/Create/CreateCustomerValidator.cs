using FluentValidation;
using Cobryx.Domain.ValueObjects;
using Cobryx.Application.Common.Validation;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode("VALIDATION.CUSTOMER.TENANT_ID.REQUIRED");
        RuleFor(v => v.FirstName)
            .NotEmpty().WithErrorCode(CustomerValidationErrors.FirstName.Required)
            .MaximumLength(100).WithErrorCode(CustomerValidationErrors.FirstName.TooLong);

        RuleFor(v => v.LastName)
            .NotEmpty().WithErrorCode(CustomerValidationErrors.LastName.Required)
            .MaximumLength(100).WithErrorCode(CustomerValidationErrors.LastName.TooLong);
        RuleFor(x => x.Phone)
            .NotEmpty().WithErrorCode(CustomerValidationErrors.Phone.Required)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithErrorCode(CustomerValidationErrors.Phone.Invalid);

        RuleFor(x => x.Address!)
            .SetValidator(new AddressValidator())
            .When(x => x.Address != null);
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty().WithErrorCode(CustomerValidationErrors.Address.StreetRequired)
            .MaximumLength(200).WithErrorCode(CustomerValidationErrors.Address.StreetTooLong);
        RuleFor(x => x.ExtNumber).NotEmpty().WithErrorCode(CustomerValidationErrors.Address.ExtNumberRequired).MaximumLength(20);
        RuleFor(x => x.ZipCode).NotEmpty().WithErrorCode(CustomerValidationErrors.Address.ZipCodeRequired).MaximumLength(10);
        RuleFor(x => x.City).NotEmpty().WithErrorCode(CustomerValidationErrors.Address.CityRequired).MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().WithErrorCode(CustomerValidationErrors.Address.StateRequired).MaximumLength(100);
    }
}

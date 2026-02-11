namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for user registration and tenant creation.
/// </summary>
public record SignUpRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName
);

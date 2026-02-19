namespace Cobryx.Api.Contracts.V1.Identity;

public record EnrollRequest(
    string Token,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    bool MarketingConsent = false);

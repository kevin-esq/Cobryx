using Cobryx.Api.Contracts.V1.Common;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for completing business onboarding details.
/// </summary>
public record OnboardRequest(
    string TaxId,
    string Industry,
    AddressContract BusinessAddress,
    string? Phone = null
);

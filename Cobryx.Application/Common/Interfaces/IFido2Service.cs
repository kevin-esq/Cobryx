using Fido2NetLib;
using Fido2NetLib.Objects;
using Cobryx.Domain.Entities;

namespace Cobryx.Application.Common.Interfaces;

public interface IFido2Service
{
    CredentialCreateOptions InitiateRegistration(User user);
    Task<MfaDevice> CompleteRegistrationAsync(User user, string deviceName, AuthenticatorAttestationRawResponse response, CredentialCreateOptions options, CancellationToken cancellationToken = default);
    AssertionOptions InitiateAssertion(User user);
    Task<bool> CompleteAssertionAsync(User user, AuthenticatorAssertionRawResponse response, AssertionOptions options, CancellationToken cancellationToken = default);
}

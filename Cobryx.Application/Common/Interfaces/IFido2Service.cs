using Cobryx.Domain.Identity;

using Fido2NetLib;

namespace Cobryx.Application.Common.Interfaces;

public interface IFido2Service
{
    public CredentialCreateOptions InitiateRegistration(User user);
    public Task<MfaDevice> CompleteRegistrationAsync(User user, string deviceName, AuthenticatorAttestationRawResponse response, CredentialCreateOptions options, CancellationToken cancellationToken = default);
    public AssertionOptions InitiateAssertion(User user);
    public Task<bool> CompleteAssertionAsync(User user, AuthenticatorAssertionRawResponse response, AssertionOptions options, CancellationToken cancellationToken = default);
}

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.Configuration;

using Fido2NetLib;
using Fido2NetLib.Objects;

using Microsoft.Extensions.Options;

namespace Cobryx.Infrastructure.Security;

public class Fido2Service : IFido2Service
{
    private readonly IFido2 _fido2;

    public Fido2Service(IOptions<Fido2Options> options)
    {
        var config = options.Value;
        var origins = new HashSet<string> { config.Origin };
        _fido2 = new Fido2(new Fido2Configuration
        {
            ServerDomain = config.ServerDomain,
            ServerName = "Cobryx",
            Origins = origins
        });
    }

    public CredentialCreateOptions InitiateRegistration(User user)
    {
        var fidoUser = new Fido2User
        {
            DisplayName = user.FullName,
            Id = user.Id.ToByteArray(),
            Name = user.Email
        };

        var existingCredentials = user.MfaDevices
            .Where(d => d.Type == MfaDeviceType.Fido2)
            .Select(d => new PublicKeyCredentialDescriptor(Convert.FromBase64String(d.CredentialId!)))
            .ToList();

        var params_ = new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None
        };

        return _fido2.RequestNewCredential(params_);
    }

    public async Task<MfaDevice> CompleteRegistrationAsync(User user, string deviceName, AuthenticatorAttestationRawResponse response, CredentialCreateOptions options, CancellationToken cancellationToken = default)
    {
        var params_ = new MakeNewCredentialParams
        {
            AttestationResponse = response,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = (args, ct) => Task.FromResult(true)
        };

        try
        {
            var success = await _fido2.MakeNewCredentialAsync(params_, cancellationToken);

            return new MfaDevice(
                user.Id,
                deviceName,
                MfaDeviceType.Fido2,
                Convert.ToBase64String(success.PublicKey),
                Convert.ToBase64String(success.Id),
                Convert.ToBase64String(success.PublicKey)
            );
        }
        catch (Fido2VerificationException)
        {
            throw new DomainException(DomainErrorCode.Auth.MfaRegistrationFailed);
        }
    }

    public AssertionOptions InitiateAssertion(User user)
    {
        var existingCredentials = user.MfaDevices
            .Where(d => d.Type == MfaDeviceType.Fido2)
            .Select(d => new PublicKeyCredentialDescriptor(Convert.FromBase64String(d.CredentialId!)))
            .ToList();

        var params_ = new GetAssertionOptionsParams
        {
            AllowedCredentials = existingCredentials,
            UserVerification = UserVerificationRequirement.Discouraged
        };

        return _fido2.GetAssertionOptions(params_);
    }

    public async Task<bool> CompleteAssertionAsync(User user, AuthenticatorAssertionRawResponse response, AssertionOptions options, CancellationToken cancellationToken = default)
    {
        var credential = user.MfaDevices.FirstOrDefault(d => d.CredentialId == response.Id);
        if (credential == null)
            return false;

        var params_ = new MakeAssertionParams
        {
            AssertionResponse = response,
            OriginalOptions = options,
            StoredPublicKey = Convert.FromBase64String(credential.PublicKey!),
            StoredSignatureCounter = credential.Counter,
            IsUserHandleOwnerOfCredentialIdCallback = (args, ct) => Task.FromResult(true)
        };

        try
        {
            var success = await _fido2.MakeAssertionAsync(params_, cancellationToken);
            credential.UpdateUsage(success.SignCount);
            return true;
        }
        catch (Fido2VerificationException)
        {
            return false;
        }
    }
}

using Fido2NetLib;
using Fido2NetLib.Objects;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Cobryx.Infrastructure.Security;

public class Fido2Service : IFido2Service
{
    private readonly IFido2 _fido2;

    public Fido2Service(IConfiguration configuration)
    {
        var origins = new HashSet<string> { configuration["Fido2:Origin"] ?? "http://localhost:3000" };
        _fido2 = new Fido2(new Fido2Configuration
        {
            ServerDomain = configuration["Fido2:ServerDomain"] ?? "localhost",
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
        catch (Fido2VerificationException ex)
        {
            throw new Exception($"FIDO2 Registration failed: {ex.Message}", ex);
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
        if (credential == null) return false;

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

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.Configuration;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cobryx.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwt;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _jwt = options.Value;
    }

    public string GenerateAccessToken(User user, Guid sessionId, Role? roleOverride = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var effectiveRole = roleOverride ?? user.Role;
        var roleName = effectiveRole?.Name ?? Role.Constants.User;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email?.Value ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(CobryxClaimTypes.TenantName, user.FullName ?? string.Empty),
            new(CobryxClaimTypes.TenantId, user.TenantId.ToString()),
            new(CobryxClaimTypes.RequiresOnboarding, user.RequiresOnboarding.ToString().ToLower()),
            new(CobryxClaimTypes.EmailVerified, user.IsEmailVerified.ToString().ToLower()),
            new(CobryxClaimTypes.SessionId, sessionId.ToString()),
            new(ClaimTypes.Role, roleName)
        };

        if (effectiveRole?.Permissions != null)
        {
            foreach (var permission in effectiveRole.Permissions)
            {
                if (permission != null && !string.IsNullOrEmpty(permission.Name))
                {
                    claims.Add(new Claim(CobryxClaimTypes.Permissions, permission.Name));
                }
            }
        }

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string GenerateMfaToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(CobryxClaimTypes.Purpose, CobryxClaimTypes.MfaVerification),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateMfaToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var purpose = jwtToken.Claims.FirstOrDefault(x => x.Type == CobryxClaimTypes.Purpose)?.Value;
            if (purpose != CobryxClaimTypes.MfaVerification) return null;

            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return null;

            return userId;
        }
        catch
        {
            return null;
        }
    }
}

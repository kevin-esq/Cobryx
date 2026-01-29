using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cobryx.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, Guid sessionId, Role? roleOverride = null)
    {
        var secretKey = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var effectiveRole = roleOverride ?? user.Role;
        var roleName = effectiveRole?.Name ?? "User";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("tenant_name", user.FullName),
            new("tenant_id", user.TenantId.ToString()),
            new("sid", sessionId.ToString()),
            new(ClaimTypes.Role, roleName)
        };

        if (effectiveRole?.Permissions != null)
        {
            foreach (var permission in effectiveRole.Permissions)
            {
                claims.Add(new Claim("permissions", permission.Name));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "15")),
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
        var secretKey = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("purpose", "mfa_verification"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateMfaToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var purpose = jwtToken.Claims.First(x => x.Type == "purpose").Value;
            if (purpose != "mfa_verification") return null;

            var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            return Guid.Parse(userId);
        }
        catch
        {
            return null;
        }
    }
}

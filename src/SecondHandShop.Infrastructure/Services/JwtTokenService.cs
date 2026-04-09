using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Security;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _keyBytes;
    private readonly double _accessTokenMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

        _keyBytes = Encoding.UTF8.GetBytes(key);

        _accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", 20.0);
        if (_accessTokenMinutes is < 5 or > 24 * 60)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 5 and 1440.");
        }
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateToken(AdminUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_accessTokenMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Restricted session: "AdminFullAccess" rejects this claim. DB remains source of truth; after forced password
        // change the API clears the cookie so the user must log in again (no immediate full-access token).
        if (user.MustChangePassword)
            claims.Add(new Claim(AdminJwtClaimTypes.PasswordChangeRequired, "true"));

        var signingKey = new SymmetricSecurityKey(_keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserService.Entities;

namespace UserService.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public int ExpiryMinutes => _expiryMinutes;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _secret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
        //hard: Enforce high-entropy symmetric key constraint (minimum 256 bits / 32 bytes)
        if (Encoding.UTF8.GetByteCount(_secret) < 32)
        {
            throw new InvalidOperationException("JwtSettings:Secret must be at least 256 bits (32 bytes) long.");
        }
        _issuer = configuration["JwtSettings:Issuer"]
            ?? "Arena.UserService";
        _audience = configuration["JwtSettings:Audience"]
            ?? "Arena.Platform";

        if (!int.TryParse(configuration["JwtSettings:ExpiryMinutes"], out _expiryMinutes) || _expiryMinutes <= 0)
        {
            _expiryMinutes = 120;
        }
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())// hard: Enforce unique GUID for every minted token to ensure unambiguous tracking in the blacklist
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
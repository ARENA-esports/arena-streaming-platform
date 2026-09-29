using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserService.Entities;
using UserService.Services;
using Xunit;

namespace UserService.Tests;

public class JwtTokenGeneratorTests
{
    private readonly IConfiguration _configuration;
    private readonly string _secret = "Arena_Secret_Key_For_Jwt_Token_Signing_2026_SE3022_Production_Grade!";
    private readonly string _issuer = "Arena.UserService";
    private readonly string _audience = "Arena.Platform";

    public JwtTokenGeneratorTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", _secret },
            { "JwtSettings:Issuer", _issuer },
            { "JwtSettings:Audience", _audience },
            { "JwtSettings:ExpiryMinutes", "120" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwt_WithAllRequiredClaims()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_configuration);
        var user = new User
        {
            UserId = 42,
            Username = "ProViewer",
            Email = "pro@arena.gg",
            Role = "Viewer"
        };

        // Act
        var tokenString = generator.GenerateToken(user);

        // Assert
        Assert.NotNull(tokenString);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Equal(_issuer, jwt.Issuer);
        Assert.Contains(_audience, jwt.Audiences);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("pro@arena.gg", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("ProViewer", jwt.Claims.First(c => c.Type == "unique_name" || c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Viewer", jwt.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value);
        Assert.NotNull(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(115));
    }

    [Fact]
    public void GenerateToken_ValidatesAgainstStreamServiceSharedSpec()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_configuration);
        var user = new User
        {
            UserId = 100,
            Username = "StreamerGuy",
            Email = "streamer@arena.gg",
            Role = "Streamer"
        };

        var tokenString = generator.GenerateToken(user);

        // TokenValidationParameters matching StreamService / Story 4 shared specification exactly
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();

        // Act
        var principal = handler.ValidateToken(tokenString, validationParameters, out var validatedToken);

        // Assert
        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.True(principal.IsInRole("Streamer"));
        Assert.NotNull(validatedToken);
    }
}

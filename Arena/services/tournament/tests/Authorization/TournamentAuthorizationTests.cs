using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TournamentService.Controllers;
using Xunit;

namespace TournamentService.Tests.Authorization;

public class TournamentAuthorizationTests
{
    [Theory]
    [InlineData("CreateTournament", typeof(HttpPostAttribute))]
    [InlineData("UpdateTournament", typeof(HttpPutAttribute))]
    [InlineData("CancelTournament", typeof(HttpPatchAttribute))]
    public void MutatingEndpoints_RequireOrganizerRole(string methodName, Type httpMethodAttributeType)
    {
        // Arrange
        var method = typeof(TournamentsController).GetMethod(methodName);
        Assert.NotNull(method);

        // Assert HTTP Method attribute is present
        var httpAttr = method.GetCustomAttribute(httpMethodAttributeType);
        Assert.NotNull(httpAttr);

        // Assert Authorize attribute is present with Roles = "Organizer"
        var authAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Organizer", authAttr.Roles);
    }

    [Theory]
    [InlineData("GetTournamentById")]
    [InlineData("GetAllTournaments")]
    public void PublicEndpoints_AllowAnonymousAccess(string methodName)
    {
        // Arrange
        var method = typeof(TournamentsController).GetMethod(methodName);
        Assert.NotNull(method);

        // Assert AllowAnonymous attribute is present
        var allowAnonymousAttr = method.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymousAttr);
    }

    [Fact]
    public void Rs256JwtValidation_ValidOrganizerToken_ValidatesSuccessfullyWithOrganizerRole()
    {
        // Arrange: Generate RSA 2048-bit key pair
        using var rsa = RSA.Create(2048);
        var rsaPrivateKey = new RsaSecurityKey(rsa) { KeyId = "test-rsa-key-1" };
        var signingCredentials = new SigningCredentials(rsaPrivateKey, SecurityAlgorithms.RsaSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "user_organizer_42"),
                new Claim("email", "organizer@arena.gg"),
                new Claim(ClaimTypes.Role, "Organizer"),
                new Claim("unique_name", "HeadOrganizer")
            }),
            Issuer = "Arena.UserService",
            Audience = "Arena.Platform",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);

        // Act: Validate token using only the RSA public key
        var rsaPublicKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = "test-rsa-key-1" };
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaPublicKey,
            ValidateIssuer = true,
            ValidIssuer = "Arena.UserService",
            ValidateAudience = true,
            ValidAudience = "Arena.Platform",
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(jwtString, validationParameters, out var validatedToken);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
        Assert.True(principal.IsInRole("Organizer"));
        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        Assert.Equal("user_organizer_42", subClaim);
    }

    [Fact]
    public void Rs256JwtValidation_NonOrganizerRole_LacksOrganizerRoleClaim()
    {
        // Arrange: Generate RS256 token with "Viewer" role
        using var rsa = RSA.Create(2048);
        var rsaPrivateKey = new RsaSecurityKey(rsa) { KeyId = "test-rsa-key-2" };
        var signingCredentials = new SigningCredentials(rsaPrivateKey, SecurityAlgorithms.RsaSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "user_viewer_99"),
                new Claim("email", "viewer@arena.gg"),
                new Claim(ClaimTypes.Role, "Viewer")
            }),
            Issuer = "Arena.UserService",
            Audience = "Arena.Platform",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);

        var rsaPublicKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = "test-rsa-key-2" };
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaPublicKey,
            ValidateIssuer = true,
            ValidIssuer = "Arena.UserService",
            ValidateAudience = true,
            ValidAudience = "Arena.Platform",
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        // Act
        var principal = tokenHandler.ValidateToken(jwtString, validationParameters, out _);

        // Assert: Viewer must NOT have Organizer role -> triggers 403 Forbidden on [Authorize(Roles = "Organizer")]
        Assert.False(principal.IsInRole("Organizer"));
        Assert.True(principal.IsInRole("Viewer"));
    }

    [Fact]
    public void Rs256JwtValidation_TamperedToken_ThrowsSecurityTokenException()
    {
        // Arrange: Generate RS256 token signed by key 1
        using var rsa1 = RSA.Create(2048);
        using var rsa2 = RSA.Create(2048); // Different untrusted key

        var rsaPrivateKey1 = new RsaSecurityKey(rsa1) { KeyId = "untrusted-key-1" };
        var signingCredentials = new SigningCredentials(rsaPrivateKey1, SecurityAlgorithms.RsaSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "malicious_user"),
                new Claim(ClaimTypes.Role, "Organizer")
            }),
            Issuer = "Arena.UserService",
            Audience = "Arena.Platform",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);

        // Validation against a different public key -> simulates invalid signature / tampering
        var untrustedPublicKey = new RsaSecurityKey(rsa2.ExportParameters(false)) { KeyId = "untrusted-key-1" };
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = untrustedPublicKey,
            ValidateIssuer = true,
            ValidIssuer = "Arena.UserService",
            ValidateAudience = true,
            ValidAudience = "Arena.Platform",
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        // Act & Assert: Should throw SecurityTokenException -> triggers 401 Unauthorized
        Assert.ThrowsAny<SecurityTokenException>(() =>
            tokenHandler.ValidateToken(jwtString, validationParameters, out _)
        );
    }
}

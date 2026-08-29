using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Moq;
using UserService.Entities;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
    private readonly Mock<ITokenBlacklistService> _mockTokenBlacklistService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        _mockTokenBlacklistService = new Mock<ITokenBlacklistService>();
        _mockJwtTokenGenerator.Setup(g => g.ExpiryMinutes).Returns(120);
        _mockJwtTokenGenerator.Setup(g => g.GenerateToken(It.IsAny<User>())).Returns("mocked.jwt.token");
        _authService = new AuthService(
            _mockRepo.Object,
            _mockJwtTokenGenerator.Object,
            _mockTokenBlacklistService.Object);
    }

    [Fact]
    public async Task Signup_WithValidData_CreatesUserAndReturnsResponse()
    {
        // Arrange
        var request = new SignupRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        _mockRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.GetByUsernameAsync(request.Username)).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);

        // Act
        var result = await _authService.SignupAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(request.Username, result.Username);
        Assert.Equal(request.Email, result.Email);
        _mockRepo.Verify(r => r.CreateUserAsync(It.Is<User>(u => 
            u.Username == request.Username && 
            u.Email == request.Email && 
            BCrypt.Net.BCrypt.Verify(request.Password, u.PasswordHash))), Times.Once);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var request = new SignupRequest { Email = "test@example.com", Username = "user", Password = "password123" };
        _mockRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new User());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.SignupAsync(request));
        Assert.Equal("Email is already registered.", ex.Message);
        _mockRepo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Signup_DuplicateUsername_ThrowsConflictException()
    {
        // Arrange
        var request = new SignupRequest { Email = "test@example.com", Username = "user", Password = "password123" };
        _mockRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.GetByUsernameAsync(request.Username)).ReturnsAsync(new User());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.SignupAsync(request));
        Assert.Equal("Username is already taken.", ex.Message);
        _mockRepo.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidUsername_ReturnsLoginResponseWithJwt()
    {
        // Arrange
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            UserId = 10,
            Username = "viewer_john",
            Email = "john@example.com",
            PasswordHash = passwordHash,
            Role = "Viewer"
        };

        var request = new LoginRequest
        {
            Identifier = "viewer_john",
            Password = password
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("viewer_john")).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.GetByUsernameAsync("viewer_john")).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mocked.jwt.token", result.Token);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(7200, result.ExpiresIn);
        Assert.Equal(10, result.UserId);
        Assert.Equal("viewer_john", result.Username);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Viewer", result.Role);
        _mockJwtTokenGenerator.Verify(g => g.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidEmail_ReturnsLoginResponseWithJwt()
    {
        // Arrange
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            UserId = 11,
            Username = "viewer_jane",
            Email = "jane@example.com",
            PasswordHash = passwordHash,
            Role = "Viewer"
        };

        var request = new LoginRequest
        {
            Identifier = "jane@example.com",
            Password = password
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("jane@example.com")).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mocked.jwt.token", result.Token);
        Assert.Equal(11, result.UserId);
        Assert.Equal("viewer_jane", result.Username);
        _mockJwtTokenGenerator.Verify(g => g.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Identifier = "nonexistent", Password = "Password123!" };
        _mockRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.GetByUsernameAsync(request.Identifier)).ReturnsAsync((User?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request));
        Assert.Equal("Invalid username/email or password.", ex.Message);
        _mockJwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var user = new User
        {
            UserId = 12,
            Username = "user12",
            Email = "user12@example.com",
            PasswordHash = passwordHash,
            Role = "Viewer"
        };

        var request = new LoginRequest { Identifier = "user12", Password = "WrongPassword!" };
        _mockRepo.Setup(r => r.GetByEmailAsync("user12")).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.GetByUsernameAsync("user12")).ReturnsAsync(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request));
        Assert.Equal("Invalid username/email or password.", ex.Message);
        _mockJwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WithValidJwtToken_RevokesToken()
    {
        // Arrange
        var expectedJti = Guid.NewGuid().ToString();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Arena_Secret_Key_For_Jwt_Token_Signing_2026_SE3022_Production_Grade!"));
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "99"),
                new Claim(JwtRegisteredClaimNames.Jti, expectedJti)
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = $"Bearer {tokenHandler.WriteToken(token)}";

        // Act
        await _authService.LogoutAsync(tokenString);

        // Assert
        _mockTokenBlacklistService.Verify(b => b.RevokeTokenAsync(expectedJti, 99, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithNullOrEmptyToken_DoesNotCallRevocation()
    {
        // Act
        await _authService.LogoutAsync(null);
        await _authService.LogoutAsync(string.Empty);
        await _authService.LogoutAsync("invalid.token");

        // Assert
        _mockTokenBlacklistService.Verify(b => b.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime>()), Times.Never);
    }
}

using System;
using System.Threading.Tasks;
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
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _authService = new AuthService(_mockRepo.Object);
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
}

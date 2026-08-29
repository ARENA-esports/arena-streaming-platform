using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
    }

    [Fact]
    public async Task Signup_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new SignupRequest { Username = "user", Email = "test@example.com", Password = "password123" };
        var response = new SignupResponse { UserId = 1, Username = "user", Email = "test@example.com" };
        _mockAuthService.Setup(s => s.SignupAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.Signup(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Signup_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Invalid email format.");
        var request = new SignupRequest();

        // Act
        var result = await _controller.Signup(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Signup_DuplicateUser_ReturnsConflict()
    {
        // Arrange
        var request = new SignupRequest { Username = "user", Email = "test@example.com", Password = "password123" };
        _mockAuthService.Setup(s => s.SignupAsync(request)).ThrowsAsync(new InvalidOperationException("Email is already registered."));

        // Act
        var result = await _controller.Signup(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task Login_ValidRequest_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest { Identifier = "viewer_user", Password = "Password123!" };
        var response = new LoginResponse
        {
            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            TokenType = "Bearer",
            ExpiresIn = 7200,
            UserId = 1,
            Username = "viewer_user",
            Email = "viewer@arena.gg",
            Role = "Viewer"
        };
        _mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Identifier", "Username or email is required.");
        var request = new LoginRequest();

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Identifier = "viewer_user", Password = "WrongPassword!" };
        _mockAuthService.Setup(s => s.LoginAsync(request)).ThrowsAsync(new UnauthorizedAccessException("Invalid username/email or password."));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }
}

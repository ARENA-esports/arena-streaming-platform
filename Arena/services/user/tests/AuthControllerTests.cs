using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public async Task Logout_ValidToken_CallsServiceAndReturnsOk()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer valid.sample.jwt";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockAuthService.Verify(s => s.LogoutAsync("Bearer valid.sample.jwt"), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "viewer@arena.gg" };
        var response = new ForgotPasswordResponse
        {
            Message = "Password reset token generated successfully.",
            ResetToken = "test-token-xyz",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _mockAuthService.Setup(s => s.ForgotPasswordAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task ForgotPassword_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Invalid email format.");
        var request = new ForgotPasswordRequest { Email = "invalid" };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ResetPasswordRequest { Token = "token-123", NewPassword = "NewPassword123!" };
        var response = new ResetPasswordResponse { Message = "Password has been successfully reset." };
        _mockAuthService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task ResetPassword_InvalidOrExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest { Token = "expired-token", NewPassword = "NewPassword123!" };
        _mockAuthService.Setup(s => s.ResetPasswordAsync(request)).ThrowsAsync(new InvalidOperationException("Invalid or expired reset token."));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("NewPassword", "Password must be at least 8 characters long.");
        var request = new ResetPasswordRequest();

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetMe_AuthenticatedUser_ReturnsUserProfile()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "25"),
            new("email", "viewer25@arena.gg"),
            new("unique_name", "Viewer25"),
            new(ClaimTypes.Role, "Viewer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = _controller.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }
}

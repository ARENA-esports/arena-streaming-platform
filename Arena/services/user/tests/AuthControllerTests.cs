using System.Security.Claims;
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
    public async Task VerifyEmail_ValidToken_ReturnsOk()
    {
        // Arrange
        var request = new VerifyEmailRequest { Token = "valid-token-xyz" };
        var response = new VerifyEmailResponse { Message = "Email has been successfully verified." };
        _mockAuthService.Setup(s => s.VerifyEmailAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task VerifyEmail_InvalidOrExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest { Token = "invalid-token" };
        _mockAuthService.Setup(s => s.VerifyEmailAsync(request)).ThrowsAsync(new InvalidOperationException("Invalid or expired verification token."));

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Token", "Verification token is required.");
        var request = new VerifyEmailRequest();

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResendVerification_ValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new ResendVerificationEmailRequest { Email = "viewer@arena.gg" };
        var response = new ResendVerificationEmailResponse { Message = "If the email is registered and unverified, a verification email has been sent." };
        _mockAuthService.Setup(s => s.ResendVerificationEmailAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.ResendVerification(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task ResendVerification_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Invalid email format.");
        var request = new ResendVerificationEmailRequest { Email = "invalid-email" };

        // Act
        var result = await _controller.ResendVerification(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsUserProfile()
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

        var expectedProfile = new UserProfileResponse
        {
            UserId = 25,
            Username = "Viewer25",
            Email = "viewer25@arena.gg",
            Role = "Viewer",
            EmailVerified = true,
            AvatarUrl = "https://cdn.arena.gg/avatars/25.png",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };
        _mockAuthService.Setup(s => s.GetProfileAsync(25)).ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedProfile, okResult.Value);
    }

    [Fact]
    public async Task GetMe_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "99")
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

        _mockAuthService.Setup(s => s.GetProfileAsync(99)).ReturnsAsync((UserProfileResponse?)null);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetMe_InvalidSubClaim_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "not-a-number")
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
        var result = await _controller.GetMe();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }
    [Fact]
    public async Task Refresh_ValidRequest_ReturnsOk()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "42")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var response = new LoginResponse
        {
            Token = "new.jwt.token",
            ExpiresIn = 7200,
            UserId = 42
        };

        _mockAuthService.Setup(s => s.RefreshTokenAsync(42)).ReturnsAsync(response);

        // Act
        var result = await _controller.Refresh();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Refresh_InvalidClaims_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>(); // Missing "sub" claim
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.Refresh();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        _mockAuthService.Verify(s => s.RefreshTokenAsync(It.IsAny<int>()), Times.Never);
    }
}

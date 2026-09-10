using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UsersController(_mockUserService.Object);
    }

    private void SetAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsUserProfile()
    {
        // Arrange
        SetAuthenticatedUser("15");
        var profile = new UserProfileResponse
        {
            UserId = 15,
            Username = "user15",
            Email = "user15@arena.gg",
            Role = "Viewer",
            EmailVerified = true
        };
        _mockUserService.Setup(s => s.GetProfileAsync(15)).ReturnsAsync(profile);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(profile, okResult.Value);
    }

    [Fact]
    public async Task GetMe_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        SetAuthenticatedUser("99");
        _mockUserService.Setup(s => s.GetProfileAsync(99)).ReturnsAsync((UserProfileResponse?)null);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ValidRequest_ReturnsOkWithProfile()
    {
        // Arrange
        SetAuthenticatedUser("15");
        var request = new UpdateProfileRequest { Username = "new_username" };
        var updatedProfile = new UserProfileResponse
        {
            UserId = 15,
            Username = "new_username",
            Email = "user15@arena.gg",
            Role = "Viewer"
        };
        _mockUserService.Setup(s => s.UpdateProfileAsync(15, request)).ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(updatedProfile, okResult.Value);
    }

    [Fact]
    public async Task UpdateProfile_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        SetAuthenticatedUser("15");
        _controller.ModelState.AddModelError("Email", "Invalid email format.");
        var request = new UpdateProfileRequest { Email = "bad-email" };

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_DuplicateUsernameOrEmail_ReturnsConflict()
    {
        // Arrange
        SetAuthenticatedUser("15");
        var request = new UpdateProfileRequest { Username = "already_taken" };
        _mockUserService.Setup(s => s.UpdateProfileAsync(15, request))
            .ThrowsAsync(new InvalidOperationException("Username is already taken."));

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        SetAuthenticatedUser("99");
        var request = new UpdateProfileRequest { Username = "new_username" };
        _mockUserService.Setup(s => s.UpdateProfileAsync(99, request))
            .ThrowsAsync(new KeyNotFoundException("User not found."));

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOkWithConfirmation()
    {
        // Arrange
        SetAuthenticatedUser("15");
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };
        var response = new ChangePasswordResponse { Message = "Password has been successfully changed." };
        _mockUserService.Setup(s => s.ChangePasswordAsync(15, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task ChangePassword_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        SetAuthenticatedUser("15");
        _controller.ModelState.AddModelError("NewPassword", "New password is required.");
        var request = new ChangePasswordRequest();

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_IncorrectCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        SetAuthenticatedUser("15");
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync(15, request))
            .ThrowsAsync(new ArgumentException("Current password is incorrect."));

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        SetAuthenticatedUser("99");
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync(99, request))
            .ThrowsAsync(new KeyNotFoundException("User not found."));

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
}

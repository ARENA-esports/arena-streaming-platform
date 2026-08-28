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
        var request = new SignupRequest(); // Missing fields intentionally for generic bad request simulation

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
        // The ConflictObjectResult value is an anonymous type, so we can't easily assert on it without reflection, 
        // but we can check the status code
        Assert.Equal(409, conflictResult.StatusCode);
    }
}

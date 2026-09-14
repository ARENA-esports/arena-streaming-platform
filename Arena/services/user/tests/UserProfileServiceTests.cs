using Moq;
using UserService.Entities;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.Tests;

public class UserProfileServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ITokenBlacklistService> _mockTokenBlacklistService;
    private readonly UserProfileService _profileService;

    public UserProfileServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTokenBlacklistService = new Mock<ITokenBlacklistService>();

        _profileService = new UserProfileService(
            _mockUserRepo.Object,
            _mockTokenBlacklistService.Object);
    }

    [Fact]
    public async Task GetProfileAsync_UserExists_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            UserId = 10,
            Username = "gamer_10",
            Email = "gamer10@arena.gg",
            Role = "Viewer",
            EmailVerified = true,
            AvatarUrl = "https://cdn.arena.gg/avatars/10.png",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow
        };
        _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

        // Act
        var result = await _profileService.GetProfileAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.UserId);
        Assert.Equal("gamer_10", result.Username);
        Assert.Equal("gamer10@arena.gg", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal("https://cdn.arena.gg/avatars/10.png", result.AvatarUrl);
    }

    [Fact]
    public async Task GetProfileAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        // Act
        var result = await _profileService.GetProfileAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidUpdate_UpdatesFieldsAndReturnsRefreshedProfile()
    {
        // Arrange
        var originalUser = new User
        {
            UserId = 10,
            Username = "old_name",
            Email = "old@arena.gg",
            Role = "Viewer",
            EmailVerified = true,
            AvatarUrl = "https://cdn.arena.gg/old.png"
        };
        var updatedUser = new User
        {
            UserId = 10,
            Username = "new_name",
            Email = "old@arena.gg",
            Role = "Viewer",
            EmailVerified = true,
            AvatarUrl = "https://cdn.arena.gg/new.png"
        };

        _mockUserRepo.SetupSequence(r => r.GetByIdAsync(10))
            .ReturnsAsync(originalUser)
            .ReturnsAsync(updatedUser);
        _mockUserRepo.Setup(r => r.GetByUsernameExcludingUserAsync("new_name", 10)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.UpdateProfileAsync(10, "new_name", "old@arena.gg", "https://cdn.arena.gg/new.png", true))
            .ReturnsAsync(true);

        var request = new UpdateProfileRequest
        {
            Username = "new_name",
            AvatarUrl = "https://cdn.arena.gg/new.png"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(10, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_name", result.Username);
        Assert.Equal("https://cdn.arena.gg/new.png", result.AvatarUrl);
        Assert.True(result.EmailVerified);
        _mockUserRepo.Verify(r => r.UpdateProfileAsync(10, "new_name", "old@arena.gg", "https://cdn.arena.gg/new.png", true), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_EmailChanged_ResetsEmailVerifiedToFalse()
    {
        // Arrange
        var originalUser = new User
        {
            UserId = 10,
            Username = "name",
            Email = "verified@arena.gg",
            Role = "Viewer",
            EmailVerified = true
        };
        var updatedUser = new User
        {
            UserId = 10,
            Username = "name",
            Email = "brandnew@arena.gg",
            Role = "Viewer",
            EmailVerified = false
        };

        _mockUserRepo.SetupSequence(r => r.GetByIdAsync(10))
            .ReturnsAsync(originalUser)
            .ReturnsAsync(updatedUser);
        _mockUserRepo.Setup(r => r.GetByEmailExcludingUserAsync("brandnew@arena.gg", 10)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.UpdateProfileAsync(10, "name", "brandnew@arena.gg", null, false))
            .ReturnsAsync(true);

        var request = new UpdateProfileRequest
        {
            Email = "brandnew@arena.gg"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(10, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("brandnew@arena.gg", result.Email);
        Assert.False(result.EmailVerified);
        _mockUserRepo.Verify(r => r.UpdateProfileAsync(10, "name", "brandnew@arena.gg", null, false), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_DuplicateUsername_ThrowsConflictException()
    {
        // Arrange
        var user = new User { UserId = 10, Username = "user10", Email = "u10@arena.gg" };
        var conflictingUser = new User { UserId = 11, Username = "taken_name", Email = "u11@arena.gg" };

        _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.GetByUsernameExcludingUserAsync("taken_name", 10)).ReturnsAsync(conflictingUser);

        var request = new UpdateProfileRequest { Username = "taken_name" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _profileService.UpdateProfileAsync(10, request));
        Assert.Equal("Username is already taken.", ex.Message);
        _mockUserRepo.Verify(r => r.UpdateProfileAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var user = new User { UserId = 10, Username = "user10", Email = "u10@arena.gg" };
        var conflictingUser = new User { UserId = 12, Username = "user12", Email = "taken@arena.gg" };

        _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.GetByEmailExcludingUserAsync("taken@arena.gg", 10)).ReturnsAsync(conflictingUser);

        var request = new UpdateProfileRequest { Email = "taken@arena.gg" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _profileService.UpdateProfileAsync(10, request));
        Assert.Equal("Email is already registered.", ex.Message);
        _mockUserRepo.Verify(r => r.UpdateProfileAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
        var request = new UpdateProfileRequest { Username = "new_name" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _profileService.UpdateProfileAsync(999, request));
        Assert.Equal("User not found.", ex.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCredentials_UpdatesPasswordAndRevokesUserTokens()
    {
        // Arrange
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewSecretPassword123!";
        var user = new User
        {
            UserId = 10,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword)
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdatePasswordAsync(10, It.IsAny<string>())).ReturnsAsync(true);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        };

        // Act
        var result = await _profileService.ChangePasswordAsync(10, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Password has been successfully changed.", result.Message);
        _mockUserRepo.Verify(r => r.UpdatePasswordAsync(10, It.Is<string>(h => BCrypt.Net.BCrypt.Verify(newPassword, h))), Times.Once);
        _mockTokenBlacklistService.Verify(t => t.RevokeUserTokensAsync(10, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsArgumentException()
    {
        // Arrange
        var user = new User
        {
            UserId = 10,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealCurrentPassword123!")
        };
        _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "IncorrectPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _profileService.ChangePasswordAsync(10, request));
        Assert.Equal("Current password is incorrect.", ex.Message);
        _mockUserRepo.Verify(r => r.UpdatePasswordAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _mockTokenBlacklistService.Verify(t => t.RevokeUserTokensAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_SameAsCurrentPassword_ThrowsArgumentException()
    {
        // Arrange
        const string password = "SamePassword123!";
        var user = new User
        {
            UserId = 10,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
        _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = password,
            NewPassword = password
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _profileService.ChangePasswordAsync(10, request));
        Assert.Equal("New password cannot be the same as the current password.", ex.Message);
        _mockUserRepo.Verify(r => r.UpdatePasswordAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _profileService.ChangePasswordAsync(999, request));
        Assert.Equal("User not found.", ex.Message);
    }
}

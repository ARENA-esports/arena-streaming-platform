using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public class UserProfileService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public UserProfileService(
        IUserRepository userRepository,
        ITokenBlacklistService tokenBlacklistService)
    {
        _userRepository = userRepository;
        _tokenBlacklistService = tokenBlacklistService;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserProfileResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            EmailVerified = user.EmailVerified,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var updatedUsername = user.Username;
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var trimmedUsername = request.Username.Trim();
            if (!string.Equals(trimmedUsername, user.Username, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userRepository.GetByUsernameExcludingUserAsync(trimmedUsername, userId);
                if (existing != null)
                {
                    throw new InvalidOperationException("Username is already taken.");
                }
                updatedUsername = trimmedUsername;
            }
        }

        var updatedEmail = user.Email;
        var emailVerified = user.EmailVerified;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var trimmedEmail = request.Email.Trim();
            if (!string.Equals(trimmedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userRepository.GetByEmailExcludingUserAsync(trimmedEmail, userId);
                if (existing != null)
                {
                    throw new InvalidOperationException("Email is already registered.");
                }
                updatedEmail = trimmedEmail;
                emailVerified = false;
            }
        }

        var updatedAvatarUrl = user.AvatarUrl;
        if (request.AvatarUrl != null)
        {
            updatedAvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        await _userRepository.UpdateProfileAsync(userId, updatedUsername, updatedEmail, updatedAvatarUrl, emailVerified);

        var refreshedUser = await _userRepository.GetByIdAsync(userId);
        return new UserProfileResponse
        {
            UserId = refreshedUser!.UserId,
            Username = refreshedUser.Username,
            Email = refreshedUser.Email,
            Role = refreshedUser.Role,
            EmailVerified = refreshedUser.EmailVerified,
            AvatarUrl = refreshedUser.AvatarUrl,
            CreatedAt = refreshedUser.CreatedAt,
            UpdatedAt = refreshedUser.UpdatedAt
        };
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ArgumentException("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("New password is required.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ArgumentException("Current password is incorrect.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new ArgumentException("New password cannot be the same as the current password.");
        }

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(userId, newPasswordHash);

        // Invalidate active JWT sessions for this user to guard against session hijacking
        await _tokenBlacklistService.RevokeUserTokensAsync(userId, DateTime.UtcNow.AddMinutes(120));

        return new ChangePasswordResponse
        {
            Message = "Password has been successfully changed."
        };
    }
}

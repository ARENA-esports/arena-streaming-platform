using UserService.Models;

namespace UserService.Services;

public interface IUserService
{
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}

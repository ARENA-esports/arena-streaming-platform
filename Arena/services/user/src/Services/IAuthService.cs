using UserService.Models;

namespace UserService.Services;

public interface IAuthService
{
    Task<SignupResponse> SignupAsync(SignupRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string? tokenString);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<ResendVerificationEmailResponse> ResendVerificationEmailAsync(ResendVerificationEmailRequest request);
    Task<LoginResponse> RefreshTokenAsync(int userId);
}

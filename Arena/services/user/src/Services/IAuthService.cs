using System.Threading.Tasks;
using UserService.Models;

namespace UserService.Services;

public interface IAuthService
{
    Task<SignupResponse> SignupAsync(SignupRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string? tokenString);
}

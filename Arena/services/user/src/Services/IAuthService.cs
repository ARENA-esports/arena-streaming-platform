using System.Threading.Tasks;
using UserService.Models;

namespace UserService.Services;

public interface IAuthService
{
    Task<SignupResponse> SignupAsync(SignupRequest request);
}

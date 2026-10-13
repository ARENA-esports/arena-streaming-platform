using System.Security.Claims;
using UserService.Models;

namespace UserService.Services;

public interface IExternalAuthService
{
    Task<string> ProcessTwitchLoginAsync(ClaimsPrincipal principal);
    Task<LoginResponse> ExchangeCodeAsync(string code);
}
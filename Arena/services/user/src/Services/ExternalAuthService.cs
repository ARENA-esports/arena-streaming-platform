using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly IExternalAuthRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IExternalAuthRepository repository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<ExternalAuthService> logger)
    {
        _repository = repository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<string> ProcessTwitchLoginAsync(ClaimsPrincipal principal)
    {
        var twitchUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Twitch user ID claim not found.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;

        var username = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? (email != null ? email.Split('@')[0] : "TwitchGamer");

        var avatar = principal.FindFirst("picture")?.Value
            ?? principal.FindFirst("avatar_url")?.Value;

        _logger.LogInformation("Processing Twitch login for ID {TwitchUserId} ({Username})", twitchUserId, username);

        // 1. Check if user already linked via Twitch
        var user = await _repository.GetUserByTwitchIdAsync(twitchUserId);

        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Twitch account has no email address or permission was denied.");
            }

            // 2. Check if an account already exists with this email
            var existingByEmail = await _userRepository.GetByEmailAsync(email);
            if (existingByEmail != null)
            {
                // Auto-link to existing email account
                user = existingByEmail;
            }
            else
            {
                // Create new user
                user = await _repository.CreateUserFromTwitchAsync(new TwitchUserInfo
                {
                    TwitchUserId = twitchUserId,
                    Username = username,
                    Email = email,
                    AvatarUrl = avatar
                });
            }
        }

        // 3. Issue single-use 60-second code
        var rawCodeBytes = RandomNumberGenerator.GetBytes(32);
        var rawCode = Convert.ToHexString(rawCodeBytes).ToLowerInvariant();
        var codeHash = ComputeSha256(rawCode);

        await _repository.SaveLoginCodeAsync(codeHash, user.UserId, DateTime.UtcNow.AddSeconds(60));

        return rawCode;
    }

    public async Task<LoginResponse> ExchangeCodeAsync(string code)
    {
        var codeHash = ComputeSha256(code);
        var userId = await _repository.ConsumeLoginCodeAsync(codeHash);

        if (userId == null)
        {
            throw new UnauthorizedAccessException("Login code is invalid or has expired.");
        }

        var user = await _userRepository.GetByIdAsync(userId.Value)
            ?? throw new UnauthorizedAccessException("User not found.");

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenGenerator.ExpiryMinutes * 60,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
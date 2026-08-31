using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using UserService.Entities;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenBlacklistService tokenBlacklistService,
        IPasswordResetRepository passwordResetRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenBlacklistService = tokenBlacklistService;
        _passwordResetRepository = passwordResetRepository;
        _configuration = configuration;
    }

    public async Task<SignupResponse> SignupAsync(SignupRequest request)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
        if (existingEmail != null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUsername != null)
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = "Viewer" // Default role
        };

        int userId = await _userRepository.CreateUserAsync(user);

        return new SignupResponse
        {
            UserId = userId,
            Username = user.Username,
            Email = user.Email,
            Message = "Signup successful. Please verify your email."
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Try finding the user by email first, then by username
        var user = await _userRepository.GetByEmailAsync(request.Identifier)
            ?? await _userRepository.GetByUsernameAsync(request.Identifier);

        // Generic error check: do not leak whether identifier or password was incorrect
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

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

    public async Task LogoutAsync(string? tokenString)
    {
        if (string.IsNullOrWhiteSpace(tokenString))
        {
            return;
        }

        if (tokenString.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            tokenString = tokenString.Substring("Bearer ".Length).Trim();
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(tokenString))
        {
            return;
        }

        var jwtToken = handler.ReadJwtToken(tokenString);
        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        int? userId = int.TryParse(sub, out var parsedId) ? parsedId : null;
        var expiresAt = jwtToken.ValidTo > DateTime.UtcNow ? jwtToken.ValidTo : DateTime.UtcNow.AddMinutes(120);

        await _tokenBlacklistService.RevokeTokenAsync(jti, userId, expiresAt);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // Do not reveal whether email exists to prevent enumeration
            return new ForgotPasswordResponse
            {
                Message = "If the email is registered, a password reset link has been sent."
            };
        }

        int expiryMinutes = _configuration.GetValue<int>("PasswordResetSettings:ExpiryMinutes", 15);
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 15;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        await _passwordResetRepository.InvalidateUserTokensAsync(user.UserId);

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _passwordResetRepository.CreateTokenAsync(resetToken);

        return new ForgotPasswordResponse
        {
            Message = "Password reset token generated successfully.",
            ResetToken = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Reset token is required.");
        }

        var resetToken = await _passwordResetRepository.GetByTokenAsync(request.Token);
        if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired reset token.");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(resetToken.UserId, passwordHash);
        await _passwordResetRepository.MarkAsUsedAsync(resetToken.Token);

        return new ResetPasswordResponse
        {
            Message = "Password has been successfully reset."
        };
    }
}

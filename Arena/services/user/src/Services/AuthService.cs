using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
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
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenBlacklistService tokenBlacklistService,
        IPasswordResetRepository passwordResetRepository,
        IEmailVerificationRepository emailVerificationRepository,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenBlacklistService = tokenBlacklistService;
        _passwordResetRepository = passwordResetRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _configuration = configuration;
        _environment = environment;
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

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = "Viewer" // Default role
        };

        var userId = await _userRepository.CreateUserAsync(user);

        // Generate email verification token upon signup
        var expiryMinutes = _configuration.GetValue<int>("EmailVerificationSettings:ExpiryMinutes", 1440);
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 1440;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var verificationToken = new EmailVerificationToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _emailVerificationRepository.CreateTokenAsync(verificationToken);

        var response = new SignupResponse
        {
            UserId = userId,
            Username = user.Username,
            Email = user.Email,
            Message = "Signup successful. Please verify your email."
        };

        if (_environment.IsDevelopment())
        {
            response.VerificationToken = token;
        }

        return response;
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
            tokenString = tokenString[7..].Trim();
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
            // Indistinguishable generic response prevents account enumeration
            return new ForgotPasswordResponse
            {
                Message = "If the email is registered, a password reset link has been sent."
            };
        }

        var expiryMinutes = _configuration.GetValue<int>("PasswordResetSettings:ExpiryMinutes", 15);
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 15;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
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

        var response = new ForgotPasswordResponse
        {
            Message = "If the email is registered, a password reset link has been sent."
        };

        // Explicitly restrict returning the raw reset token to local development/testing environments
        if (_environment.IsDevelopment())
        {
            response.ResetToken = token;
            response.ExpiresAt = expiresAt;
        }

        return response;
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

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(resetToken.UserId, passwordHash);
        await _passwordResetRepository.MarkAsUsedAsync(resetToken.Token);

        // Invalidate all existing active JWT sessions for this user after password change
        await _tokenBlacklistService.RevokeUserTokensAsync(resetToken.UserId, DateTime.UtcNow.AddMinutes(120));

        return new ResetPasswordResponse
        {
            Message = "Password has been successfully reset."
        };
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

    public async Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Verification token is required.");
        }

        var verificationToken = await _emailVerificationRepository.GetByTokenAsync(request.Token);
        if (verificationToken == null || verificationToken.IsUsed || verificationToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired verification token.");
        }

        await _userRepository.VerifyEmailAsync(verificationToken.UserId);
        await _emailVerificationRepository.MarkAsUsedAsync(verificationToken.Token);

        return new VerifyEmailResponse
        {
            Message = "Email has been successfully verified."
        };
    }

    public async Task<ResendVerificationEmailResponse> ResendVerificationEmailAsync(ResendVerificationEmailRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || user.EmailVerified)
        {
            // Generic response prevents account enumeration
            return new ResendVerificationEmailResponse
            {
                Message = "If the email is registered and unverified, a verification email has been sent."
            };
        }

        var expiryMinutes = _configuration.GetValue<int>("EmailVerificationSettings:ExpiryMinutes", 1440);
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 1440;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        await _emailVerificationRepository.InvalidateUserTokensAsync(user.UserId);

        var verificationToken = new EmailVerificationToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _emailVerificationRepository.CreateTokenAsync(verificationToken);

        var response = new ResendVerificationEmailResponse
        {
            Message = "If the email is registered and unverified, a verification email has been sent."
        };

        if (_environment.IsDevelopment())
        {
            response.VerificationToken = token;
        }

        return response;
    }
}
    public async Task<LoginResponse> RefreshTokenAsync(int userId)
    {
        // retrieve user from database to ensure they still exist
        var user = await _userRepository.GetByIdAsync(userId);
        
        // null check for deleted user
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found or has been deleted.");
        }

        // issue fresh jwt to extend session
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
}

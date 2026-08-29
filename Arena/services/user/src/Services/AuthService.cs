using System;
using System.Threading.Tasks;
using BCrypt.Net;
using UserService.Entities;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
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
}

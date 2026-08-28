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

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
}

using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class UpdateProfileRequest
{
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    public string? Username { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string? Email { get; set; }

    [Url(ErrorMessage = "Avatar URL must be a valid URL.")]
    [MaxLength(255, ErrorMessage = "Avatar URL cannot exceed 255 characters.")]
    public string? AvatarUrl { get; set; }
}

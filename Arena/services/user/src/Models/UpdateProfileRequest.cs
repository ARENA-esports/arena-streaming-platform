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

    public string? AvatarUrl { get; set; }

    [MaxLength(50, ErrorMessage = "Display name cannot exceed 50 characters.")]
    public string? DisplayName { get; set; }

    [MaxLength(300, ErrorMessage = "Bio cannot exceed 300 characters.")]
    public string? Bio { get; set; }

    public string? BannerUrl { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class ResendVerificationEmailRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
}

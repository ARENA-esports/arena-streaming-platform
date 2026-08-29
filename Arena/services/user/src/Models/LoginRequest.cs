using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class LoginRequest
{
    /// <summary>
    /// The username or email of the user attempting to log in.
    /// </summary>
    [Required(ErrorMessage = "Username or email is required.")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// The plain-text password to authenticate with.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

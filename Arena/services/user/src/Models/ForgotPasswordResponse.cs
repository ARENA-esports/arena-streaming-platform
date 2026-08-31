namespace UserService.Models;

public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

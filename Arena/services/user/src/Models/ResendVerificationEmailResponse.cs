namespace UserService.Models;

public class ResendVerificationEmailResponse
{
    public string Message { get; set; } = string.Empty;
    public string? VerificationToken { get; set; }
}

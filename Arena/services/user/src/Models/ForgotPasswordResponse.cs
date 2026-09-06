using System.Text.Json.Serialization;

namespace UserService.Models;

public class ForgotPasswordResponse
{
    public string Message { get; set; } = "If the email is registered, a password reset link has been sent.";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResetToken { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExpiresAt { get; set; }
}

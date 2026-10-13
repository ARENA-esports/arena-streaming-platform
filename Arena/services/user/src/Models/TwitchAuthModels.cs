using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class TwitchExchangeRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class TwitchUserInfo
{
    public string TwitchUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
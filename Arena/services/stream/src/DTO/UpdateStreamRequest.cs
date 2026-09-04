using System.ComponentModel.DataAnnotations;

namespace StreamService.DTOs;

public class UpdateStreamRequest
{
    [Required(ErrorMessage = "Channel name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Channel name must be between 1 and 100 characters.")]
    public string ChannelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Platform is required.")]
    [RegularExpression("^(Twitch|YouTube)$", ErrorMessage = "Platform must be either 'Twitch' or 'YouTube'.")]
    public string Platform { get; set; } = "Twitch";

    [StringLength(255, ErrorMessage = "Stream title cannot exceed 255 characters.")]
    public string StreamTitle { get; set; } = "Arena Live Match Broadcast";

    [Required(ErrorMessage = "Embed parent domain is required for Twitch ToS compliance.")]
    [RegularExpression(@"^(?!https?:\/\/)([a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$|^localhost$", 
        ErrorMessage = "Parent domain must be a valid hostname (e.g., 'localhost' or 'arena.gg') without http://, https://, ports, or trailing slashes.")]
    public string EmbedParentDomain { get; set; } = "localhost";
}
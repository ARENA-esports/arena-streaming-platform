using System.ComponentModel.DataAnnotations;    // import built in validation attributes


namespace StreamService.DTOs;   //use file scope

public class LinkStreamRequest
{
    [Required(ErrorMessage = "Chanel name is required.")]   // ensure client can't pass null or empty string
    [StringLength(100, MinimumLength = 1, ErrorMessage="Channel name must between 1 and 100 characters.")]  //match database column length and prevent oversize
    public string ChannelName {get; set;} =string.Empty;         // initialize property with not null default

    [Required(ErrorMessage = "Platform is required.")]      // guarantee property is present
    [RegularExpression("^(Twitch|YouTube)$", ErrorMessage="Platform must be either 'Twitch' or 'YouTube'.")]    // restrict acceptable values to 'Twitch' or 'YouTube' to match enums.
    public string Platform {get; set;} = "Twitch";          // set default platform to twitch

    [StringLength(255, ErrorMessage="Stream title cannot exceed 255 characters.")]      // enforce db column limit
    public string StreamTitle {get; set;} = "Arena Live Match Broadcast";           // provide fallback title matching default

    [Required(ErrorMessage="Embed parent domain is required for Twitch ToS compliance.")]   // prevent parent parameter omitting
    /*
        use negative lookaheads (?!https?:\/\/) and standard hostname rules.
        accept valid domains while blocking protocol prefixes, ports, paths
    */
    [RegularExpression(@"^(?!https?:\/\/)([a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$|^localhost$",
        ErrorMessage = "Parent domain must be a valid hostname (e.g., 'localhost' or 'arena.gg') without http://, https://, ports, or trailing slashes.")]
    public string EmbedParentDomain {get; set;} = "localhost";      // set default for local docker development
    
}
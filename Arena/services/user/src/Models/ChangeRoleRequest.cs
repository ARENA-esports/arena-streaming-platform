using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class ChangeRoleRequest
{
    [Required]
    [RegularExpression("^(Viewer|Organizer|Streamer|Admin)$", ErrorMessage = "Role must be Viewer, Organizer, Streamer, or Admin.")]
    public string Role { get; set; } = string.Empty;
}

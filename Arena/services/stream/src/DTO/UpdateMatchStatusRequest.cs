using System.ComponentModel.DataAnnotations;

namespace StreamService.DTOs;

public class UpdateMatchStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Scheduled|Live|Ended|Cancelled)$", ErrorMessage = "Status must be one of: Scheduled, Live, Ended, Cancelled.")]
    public string Status { get; set; } = string.Empty;

    public bool ForceOverride { get; set; } = false;
}


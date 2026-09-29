using System.ComponentModel.DataAnnotations;

namespace ChatService.Models;

public class MuteUserRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be a positive integer.")]
    public int UserId { get; set; }

    [Required]
    [Range(1, 10080, ErrorMessage = "DurationMinutes must be between 1 and 10080 (7 days).")]
    public int DurationMinutes { get; set; }

    public string? Reason { get; set; }
}

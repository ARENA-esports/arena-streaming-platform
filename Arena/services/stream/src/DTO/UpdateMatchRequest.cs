using System.ComponentModel.DataAnnotations;

namespace StreamService.DTOs;

public class UpdateMatchRequest
{
    [Required(ErrorMessage = "Team A ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Team A ID must be a valid positive integer.")]
    public int TeamAId { get; set; }

    [Required(ErrorMessage = "Team B ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Team B ID must be a valid positive integer.")]
    public int TeamBId { get; set; }

    [Required(ErrorMessage = "Scheduled time is required.")]
    public DateTimeOffset ScheduledTime { get; set; }
}


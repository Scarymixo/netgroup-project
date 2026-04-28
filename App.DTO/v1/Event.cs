using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1;

public class Event
{
    public Guid Id { get; set; }

    [StringLength(128, MinimumLength = 1)]
    public string EventName { get; set; } = default!;
    
    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

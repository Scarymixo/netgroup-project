using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using Base.Domain;

namespace App.Domain;

public class Event : BaseEntity
{
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    [StringLength(128, MinimumLength = 1)] 
    public string EventName { get; set; } = default!;
    
    public int MaxParticipants { get; set; }
    
    public virtual DateTime StartTime { get; set; }
    public virtual DateTime EndTime { get; set; }
    
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual DateTime? UpdatedAt { get; set; }
    
    public ICollection<Participant>? Participants { get; set; }
}
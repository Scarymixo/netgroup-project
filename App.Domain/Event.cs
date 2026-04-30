using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using Base.Domain;

namespace App.Domain;

public class Event : BaseEntity, IValidatableObject
{
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    [StringLength(128, MinimumLength = 1)]
    public string EventName { get; set; } = default!;

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    public virtual DateTime StartTime { get; set; }
    public virtual DateTime EndTime { get; set; }

    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual DateTime? UpdatedAt { get; set; }

    public ICollection<Participant>? Participants { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime < DateTime.UtcNow.AddHours(1))
        {
            yield return new ValidationResult(
                "StartTime must be at least 1 hour in the future.",
                new[] { nameof(StartTime) });
        }

        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "EndTime must be after StartTime.",
                new[] { nameof(EndTime) });
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1;

public class Event : IValidatableObject
{
    public Guid Id { get; set; }

    [StringLength(128, MinimumLength = 1)]
    public string EventName { get; set; } = default!;

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime <= DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "StartTime must be in the future.",
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

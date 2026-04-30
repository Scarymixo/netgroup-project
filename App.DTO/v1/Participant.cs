using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1;

public class Participant
{
    public Guid Id { get; set; }

    [Required]
    public Guid EventId { get; set; }

    [StringLength(50, MinimumLength = 1)]
    [Required]
    public string FirstName { get; set; } = default!;

    [StringLength(50, MinimumLength = 1)]
    [Required]
    public string LastName { get; set; } = default!;

    [StringLength(11, MinimumLength = 11)]
    [RegularExpression(@"^\d+$", ErrorMessage = "National ID must contain only digits.")]
    [Required]
    public string NationalId { get; set; } = default!;
}

using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1;

public class Participant
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = default!;

    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = default!;

    [StringLength(25, MinimumLength = 1)]
    public string NationalId { get; set; } = default!;
}

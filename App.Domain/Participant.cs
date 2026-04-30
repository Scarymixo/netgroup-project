using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Participant : BaseEntity
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }
    
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
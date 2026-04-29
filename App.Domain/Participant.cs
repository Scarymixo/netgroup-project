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
    
    [StringLength(25, MinimumLength = 1)]
    [Required]
    public string NationalId { get; set; } = default!;
}
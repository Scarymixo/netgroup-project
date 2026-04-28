using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace App.Domain.Identity;

public class AppUserRole : IdentityUserRole<Guid>
{
    [ForeignKey(nameof(IdentityUserRole<>.UserId))]
    public AppUser AppUser { get; set; } = default!;

    [ForeignKey(nameof(IdentityUserRole<>.RoleId))]
    public AppRole AppRole { get; set; }= default!;
}
using App.Domain;
using App.Domain.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IDataProtectionKeyContext
{
    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;

    public DbSet<Event> Events { get; set; }
    public DbSet<Participant> Participants { get; set; }
    
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // disable cascade delete
        // foreach (var relationship in builder.Model
        //              .GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        // {
        //     relationship.DeleteBehavior = DeleteBehavior.Restrict;
        // }
        
        builder.Entity<Participant>()
            .HasIndex(p => new { p.EventId, p.NationalId })
            .IsUnique();
    }
}
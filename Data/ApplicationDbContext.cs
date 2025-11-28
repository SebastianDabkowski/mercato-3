using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Data;

/// <summary>
/// Database context for the Mercato application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the users table.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user sessions table.
    /// Sessions are stored in the database to support horizontal scaling.
    /// </summary>
    public DbSet<UserSession> UserSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            // Index on token for fast lookups during validation
            entity.HasIndex(e => e.Token).IsUnique();
            
            // Index on user ID for invalidating all user sessions
            entity.HasIndex(e => e.UserId);
            
            // Index for cleanup of expired sessions
            entity.HasIndex(e => new { e.IsValid, e.ExpiresAt });

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

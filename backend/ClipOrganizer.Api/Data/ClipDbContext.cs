using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Data;

public class ClipDbContext : DbContext
{
    public ClipDbContext(DbContextOptions<ClipDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clip> Clips { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Clip entity
        modelBuilder.Entity<Clip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);
            entity.Property(e => e.LocationString).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Duration).IsRequired();
            
            // Create unique index on LocationString to prevent duplicates
            entity.HasIndex(e => e.LocationString).IsUnique();
        });

        // Configure Tag entity
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(200);
            
            // Create unique index on Category + Value to prevent duplicates
            entity.HasIndex(e => new { e.Category, e.Value }).IsUnique();
        });

        // Configure many-to-many relationship
        modelBuilder.Entity<Clip>()
            .HasMany(c => c.Tags)
            .WithMany(t => t.Clips)
            .UsingEntity(j => j.ToTable("ClipTags"));
    }
}


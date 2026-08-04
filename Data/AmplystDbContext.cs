using amplyst_spotify_api.Models.Core;
using amplyst_spotify_api.Common;
using amplyst_spotify_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace amplyst_spotify_api.Data;

public class AmplystDbContext(DbContextOptions<AmplystDbContext> options) : DbContext(options)
{
    public DbSet<ImportJob> ImportJobs { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<PlaylistItem> PlaylistItems { get; set; }
    public DbSet<Artist> Artists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportJob>(entity => entity.Property(s => s.Status).HasConversion<string>());
        modelBuilder.Entity<Item>().HasMany(i => i.Artists).WithMany();
    }

    public override int SaveChanges()
    {
        ApplyAuditMetadata();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditMetadata();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditMetadata()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Auditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
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
        modelBuilder.Entity<ImportJob>(static entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(s => s.Status).HasConversion<string>();
            entity.ToTable("ImportJobs");
        });

        modelBuilder.Entity<Playlist>(static entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(p => p.SpotifyPlaylistUri).IsUnique();
            entity.Property(p => p.SpotifySnapshotId).IsConcurrencyToken();
            entity.ToTable("Playlists");
        });

        modelBuilder.Entity<Item>(static entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(i => i.SpotifyItemUri).IsUnique();
            entity.Property(i => i.SpotifyItemUri).UseCollation("Latin1_General_CS_AS");
            entity.HasMany(i => i.Artists).WithMany().UsingEntity(
                "ItemArtists",
                l => l.HasOne(typeof(Artist)).WithMany().HasForeignKey("ArtistId"),
                r => r.HasOne(typeof(Item)).WithMany().HasForeignKey("ItemId"),
                j => j.HasKey("ItemId", "ArtistId")
            );
            entity.ToTable("Items");
        });

        modelBuilder.Entity<PlaylistItem>(static entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(pi => new { pi.PlaylistId, pi.ItemId }).IsUnique(false);
            entity.HasIndex(pi => pi.ItemId);
            entity.ToTable("PlaylistItems");
        });

        modelBuilder.Entity<Artist>(static entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(a => a.SpotifyArtistUri).IsUnique();
            entity.Property(a => a.SpotifyArtistUri).HasMaxLength(100);
            entity.Property(a => a.SpotifyArtistUri).UseCollation("Latin1_General_CS_AS");
            entity.ToTable("Artists");
        });
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
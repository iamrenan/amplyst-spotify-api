using amplyst_spotify_api.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace amplyst_spotify_api.Data;

public class AmplystDbContext(DbContextOptions<AmplystDbContext> options) : DbContext(options)
{


    public DbSet<SyncData> Syncs { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncData>()
            .HasKey(s => s.SyncRunId);
        modelBuilder.Entity<SyncData>()
            .Property(s => s.Status)
            .HasConversion<string>();
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
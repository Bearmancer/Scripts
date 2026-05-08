using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
    : DbContext(options)
{
    public DbSet<Entities.Artist> Artists => Set<Entities.Artist>();
    public DbSet<Entities.Album> Albums => Set<Entities.Album>();
    public DbSet<Entities.Track> Tracks => Set<Entities.Track>();
    public DbSet<Entities.Scrobble> Scrobbles => Set<Entities.Scrobble>();
    public DbSet<Entities.ExecutionLog> ExecutionLogs => Set<Entities.ExecutionLog>();
    public DbSet<Entities.FiberyEntity> FiberyEntities => Set<Entities.FiberyEntity>();
    public DbSet<Entities.FailedTask> FailedTasks => Set<Entities.FailedTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Artists ──
        modelBuilder.Entity<Entities.Artist>(entity =>
        {
            entity.ToTable("artists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Mbid);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => e.Name).HasDatabaseName("idx_artists_name");
        });

        // ── Albums ──
        modelBuilder.Entity<Entities.Album>(entity =>
        {
            entity.ToTable("albums");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.ReleaseDate).HasColumnType("date");
            entity.Property(e => e.Mbid);
            entity.HasOne(e => e.Artist)
                  .WithMany(a => a.Albums)
                  .HasForeignKey(e => e.ArtistId)
                  .IsRequired();
            entity.HasIndex(e => e.Title).HasDatabaseName("idx_albums_title");
        });

        // ── Tracks ──
        modelBuilder.Entity<Entities.Track>(entity =>
        {
            entity.ToTable("tracks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Mbid);
            entity.HasOne(e => e.Album)
                  .WithMany(a => a.Tracks)
                  .HasForeignKey(e => e.AlbumId)
                  .IsRequired();
            entity.HasOne(e => e.Artist)
                  .WithMany(a => a.Tracks)
                  .HasForeignKey(e => e.ArtistId)
                  .IsRequired();
            entity.HasIndex(e => e.Title).HasDatabaseName("idx_tracks_title");
        });

        // ── Scrobbles ──
        modelBuilder.Entity<Entities.Scrobble>(entity =>
        {
            entity.ToTable("scrobbles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Timestamp).HasColumnType("timestamptz").IsRequired();
            entity.Property(e => e.Platform).IsRequired();
            entity.HasOne(e => e.Track)
                  .WithMany(t => t.Scrobbles)
                  .HasForeignKey(e => e.TrackId)
                  .IsRequired();
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("idx_scrobbles_timestamp");
        });

        // ── Execution Logs ──
        modelBuilder.Entity<Entities.ExecutionLog>(entity =>
        {
            entity.ToTable("execution_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Timestamp).HasColumnType("timestamptz")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Payload).HasColumnType("jsonb");
        });

        // ── Fibery Entities ──
        modelBuilder.Entity<Entities.FiberyEntity>(entity =>
        {
            entity.ToTable("fibery_entities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FiberyId).IsRequired();
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.RawData).HasColumnType("jsonb");
        });

        // ── Failed Tasks ──
        modelBuilder.Entity<Entities.FailedTask>(entity =>
        {
            entity.ToTable("failed_tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TaskName).IsRequired();
            entity.Property(e => e.Timestamp).HasColumnType("timestamptz")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

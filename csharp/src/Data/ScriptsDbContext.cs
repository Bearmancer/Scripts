using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data.Entities;
using Scrobble = CSharpScripts.Data.Entities.Scrobble;
using Track = CSharpScripts.Data.Entities.Track;

namespace CSharpScripts.Data;

/// <summary>
/// Primary EF Core DbContext for the Scripts application.
/// NoTracking is the default; enable tracking explicitly per-operation when needed.
/// Entity type configurations are loaded from assembly in OnModelCreating.
/// </summary>
public sealed class ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
    : DbContext(options)
{
    // Music domain
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Scrobble> Scrobbles => Set<Scrobble>();
    public DbSet<Video> Videos => Set<Video>();

    // Management domain
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<FailedTask> FailedTasks => Set<FailedTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<System.Text.Json.JsonDocument>();
        // Entity configurations will be loaded here in subsequent plans (03-dbcontext-config)
    }
}

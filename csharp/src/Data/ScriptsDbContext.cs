#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data.Entities;
using EntityScrobble = CSharpScripts.Data.Entities.Scrobble;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
		: base(options: options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Album> Albums => Set<Album>();
	public DbSet<Track> Tracks => Set<Track>();
	public DbSet<EntityScrobble> Scrobbles => Set<EntityScrobble>();
	public DbSet<Video> Videos => Set<Video>();

	public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
	public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
	public DbSet<FailedTask> FailedTasks => Set<FailedTask>();
	public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();

	protected override void OnModelCreating(ModelBuilder mb)
	{
		mb.Ignore<System.Text.Json.JsonDocument>();
		mb.ApplyConfigurationsFromAssembly(assembly: typeof(ScriptsDbContext).Assembly);
	}
}

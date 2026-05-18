#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options) : base(options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<Entities.Artist> Artists => Set<Entities.Artist>();
	public DbSet<Entities.Album> Albums => Set<Entities.Album>();
	public DbSet<Entities.Track> Tracks => Set<Entities.Track>();
	public DbSet<Entities.Scrobble> Scrobbles => Set<Entities.Scrobble>();
	public DbSet<Entities.Video> Videos => Set<Entities.Video>();

	public DbSet<Entities.ExecutionLog> ExecutionLogs => Set<Entities.ExecutionLog>();
	public DbSet<Entities.FiberyEntity> FiberyEntities => Set<Entities.FiberyEntity>();
	public DbSet<Entities.FailedTask> FailedTasks => Set<Entities.FailedTask>();

	protected override void OnModelCreating(ModelBuilder mb)
		=> mb.ApplyConfigurationsFromAssembly(typeof(ScriptsDbContext).Assembly);
}

using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
		: base(options: options) =>
		ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Album> Albums => Set<Album>();
	public DbSet<Track> Tracks => Set<Track>();
	public DbSet<Entities.Scrobble> Scrobbles => Set<Entities.Scrobble>();
	public DbSet<Video> Videos => Set<Video>();

	public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
	public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
	public DbSet<FailedTask> FailedTasks => Set<FailedTask>();
	public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();
	public DbSet<ReleaseProgress> ReleaseProgress => Set<ReleaseProgress>();

	protected override void OnModelCreating(ModelBuilder mb)
	{
		mb.ApplyConfiguration(new Configuration.ArtistConfiguration());
		mb.ApplyConfiguration(new Configuration.AlbumConfiguration());
		mb.ApplyConfiguration(new Configuration.TrackConfiguration());
		mb.ApplyConfiguration(new Configuration.ScrobbleConfiguration());
		mb.ApplyConfiguration(new Configuration.VideoConfiguration());
		mb.ApplyConfiguration(new Configuration.ExecutionLogConfiguration());
		mb.ApplyConfiguration(new Configuration.FiberyEntityConfiguration());
		mb.ApplyConfiguration(new Configuration.FailedTaskConfiguration());
		mb.ApplyConfiguration(new Configuration.SourceRecordConfiguration());
		mb.ApplyConfiguration(new Configuration.ReleaseProgressConfiguration());

		if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
		{
			var jsonConverter =
				new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<
					System.Text.Json.JsonDocument,
					string
				>(
					v => v.RootElement.ToString(),
					v => System.Text.Json.JsonDocument.Parse(
						v,
						new System.Text.Json.JsonDocumentOptions()
					)
				);

			foreach (var entityType in mb.Model.GetEntityTypes())
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(System.Text.Json.JsonDocument))
					property.SetValueConverter(jsonConverter);
			}
		}
	}
}


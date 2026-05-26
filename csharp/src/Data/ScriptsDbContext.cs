#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using EntityScrobble = CSharpScripts.Data.Entities.Scrobble;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	private static bool IsTestContext =>
		AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Scripts.Tests");

	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
		: base(options: options) =>
		ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Album> Albums => Set<Album>();
	public DbSet<Track> Tracks => Set<Track>();
	public DbSet<EntityScrobble> Scrobbles => Set<EntityScrobble>();
	public DbSet<Video> Videos => Set<Video>();

	public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
	public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
	public DbSet<FailedTask> FailedTasks => Set<FailedTask>();
	public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);

		if (!IsTestContext)
			optionsBuilder.UseModel(ScriptsDbContextModel.Instance);
	}

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

		if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
		{
			var jsonConverter =
				new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<
					System.Text.Json.JsonDocument,
					string
				>(
					v => v.RootElement.ToString(),
					v =>
						System.Text.Json.JsonDocument.Parse(
							v,
							new System.Text.Json.JsonDocumentOptions()
						)
				);

			foreach (var entityType in mb.Model.GetEntityTypes())
			{
				foreach (var property in entityType.GetProperties())
				{
					if (property.ClrType == typeof(System.Text.Json.JsonDocument))
					{
						property.SetValueConverter(jsonConverter);
					}
				}
			}
		}
	}
}

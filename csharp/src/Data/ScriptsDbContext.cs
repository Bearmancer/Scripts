using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Scripts.Data.Entities;

namespace Scripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
		: base(options: options) =>
		ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);

		var inMemory = optionsBuilder.Options.Extensions.Any(e =>
			e.GetType().FullName?.Contains("InMemoryOptionsExtension") == true
		);
		var noCompiledModel =
			System.Environment.GetEnvironmentVariable("SCRIPTS_NO_COMPILED_MODEL") is not null;
		if (inMemory || noCompiledModel)
			return;
		try
		{
			optionsBuilder.UseModel(MyCompiledModels.ScriptsDbContextModel.Instance);
		}
		catch (Exception)
		{
			throw;
		}
	}

	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Album> Albums => Set<Album>();
	public DbSet<Track> Tracks => Set<Track>();
	public DbSet<Scrobble> Scrobbles => Set<Scrobble>();
	public DbSet<Video> Videos => Set<Video>();
	public DbSet<Playlist> Playlists => Set<Playlist>();
	public DbSet<PlaylistVideo> PlaylistVideos => Set<PlaylistVideo>();

	public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
	public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();
	public DbSet<ReleaseProgress> ReleaseProgress => Set<ReleaseProgress>();

	protected override void OnModelCreating(ModelBuilder mb)
	{
		mb.ApplyConfiguration(new Configuration.ArtistConfiguration());
		mb.ApplyConfiguration(new Configuration.AlbumConfiguration());
		mb.ApplyConfiguration(new Configuration.TrackConfiguration());
		mb.ApplyConfiguration(new Configuration.ScrobbleConfiguration());
		mb.ApplyConfiguration(new Configuration.VideoConfiguration());
		mb.ApplyConfiguration(new Configuration.PlaylistConfiguration());
		mb.ApplyConfiguration(new Configuration.PlaylistVideoConfiguration());
		mb.ApplyConfiguration(new Configuration.FiberyEntityConfiguration());
		mb.ApplyConfiguration(new Configuration.SourceRecordConfiguration());
		mb.ApplyConfiguration(new Configuration.ReleaseProgressConfiguration());

		foreach (var entityType in mb.Model.GetEntityTypes())
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(string))
				{
					var comparer = new ValueComparer<string>(
						(l, r) => string.Equals(l, r, StringComparison.Ordinal),
						v => v == null ? 0 : StringComparer.Ordinal.GetHashCode(v),
						v => v
					);
					property.SetValueComparer(comparer);
				}
			}

		if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
		{
			var jsonConverter = new ValueConverter<JsonDocument, string>(
				v => v.RootElement.ToString(),
				v => JsonDocument.Parse(v, new JsonDocumentOptions())
			);

			foreach (var entityType in mb.Model.GetEntityTypes())
				foreach (var property in entityType.GetProperties())
				{
					if (property.ClrType == typeof(JsonDocument))
						property.SetValueConverter(jsonConverter);
				}
		}
	}
}

using System.Text.Json;
using Scripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Scripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
		: base(options: options) =>
		ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
		// Compiled model breaks InMemory provider (missing key comparers); use OnModelCreating instead.
		var inMemory = optionsBuilder.Options.Extensions
			.Any(e => e.GetType().FullName?.Contains("InMemoryOptionsExtension") == true);
		var noCompiledModel = System.Environment.GetEnvironmentVariable("SCRIPTS_NO_COMPILED_MODEL") is not null;
		Console.WriteLine($"[SCRIPTS_CTX] OnConfiguring: inMemory={inMemory} noCompiledModel={noCompiledModel}");
		if (inMemory || noCompiledModel)
			return;
		try
		{
			optionsBuilder.UseModel(MyCompiledModels.ScriptsDbContextModel.Instance);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[SCRIPTS] UseModel FAILED: {ex.GetType().Name}: {ex.Message}");
			throw;
		}
	}

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
			var jsonConverter = new ValueConverter<JsonDocument, string>(
				v => v.RootElement.ToString(),
				v => JsonDocument.Parse(v, new JsonDocumentOptions()));

			foreach (var entityType in mb.Model.GetEntityTypes())
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(JsonDocument))
					property.SetValueConverter(jsonConverter);
			}
		}
		else
		{
			// EF Core 10.0.8 OriginalValuesFactoryFactory NRE workaround:
			// explicit value comparers prevent the lazy-init race in GetValueComparer.
			// The hash function MUST be null-safe so materialising a row with a
			// nullable string column (e.g. ReleaseProgress.Composer) does not throw
			// NullReferenceException from inside the non-capturing lazy initializer.
			// Equality is ordinal to match the previous semantics.
			foreach (var entityType in mb.Model.GetEntityTypes())
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(string))
				{
					var comparer = new ValueComparer<string>(
						(l, r) => string.Equals(l, r, StringComparison.Ordinal),
						v => v == null ? 0 : StringComparer.Ordinal.GetHashCode(v),
						v => v);
					property.SetValueComparer(comparer);
				}
			}
		}
	}
}

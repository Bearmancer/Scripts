using Microsoft.EntityFrameworkCore;
using Scripts.Data;

namespace Scripts.Tests.ReleaseProgress;

internal sealed class ReleaseProgressConfigurationTests
{
	[Test]
	public async Task ReleaseProgress_HasCorrectTableName()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(
			typeof(Data.Entities.ReleaseProgress)
		);

		await Assert.That(entityType).IsNotNull();
		await Assert.That(entityType!.GetTableName()).IsEqualTo("release_progress");
	}

	[Test]
	public async Task ReleaseProgress_HasCompositeUniqueIndex()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(
			typeof(Data.Entities.ReleaseProgress)
		);

		var indexes = entityType!.GetIndexes().ToList();
		await Assert
			.That(
				indexes.Any(i =>
					i.Properties.Any(p => p.Name == "ReleaseId")
					&& i.Properties.Any(p => p.Name == "DiscNumber")
					&& i.Properties.Any(p => p.Name == "TrackNumber")
					&& i.IsUnique
				)
			)
			.IsTrue();
	}

	[Test]
	public async Task ReleaseProgress_Soloists_IsJsonb()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseNpgsql("Host=localhost;Database=dummy;Username=dummy;Password=dummy")
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(
			typeof(Data.Entities.ReleaseProgress)
		);
		var prop = entityType!.FindProperty("Soloists");

		await Assert.That(prop).IsNotNull();
		await Assert.That(prop!.GetColumnType()).IsEqualTo("jsonb");
	}
}

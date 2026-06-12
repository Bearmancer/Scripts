using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class TrackConfigurationTests
{
	[Test]
	public async Task Track_Duration_ColumnType_IsInteger()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Track));
		var prop = entityType!.FindProperty("DurationSeconds");

		await Assert.That(prop).IsNotNull();
		await Assert
			.That(
				prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value
			)
			.IsEqualTo("integer");
	}

	[Test]
	public async Task Track_HasCompositeUnique_OnArtistIdAndTitle()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Track));
		var indexes = entityType!.GetIndexes().ToList();

		await Assert
			.That(indexes)
			.Contains(i =>
				i.Properties.Any(p => p.Name == "ArtistId")
				&& i.Properties.Any(p => p.Name == "Title")
				&& i.IsUnique
			);
	}
}

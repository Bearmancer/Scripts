using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class AlbumConfigurationTests
{
	[Test]
	public async Task Album_ReleaseDate_ColumnType_IsDate()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Album));
		var prop = entityType!.FindProperty("ReleaseDate");

		await Assert.That(prop).IsNotNull();
		await Assert
			.That(
				prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value
			)
			.IsEqualTo("date");
	}

	[Test]
	public async Task Album_HasReleaseDate_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Album));
		var indexes = entityType!.GetIndexes().ToList();

		await Assert.That(indexes).Contains(i => i.Properties.Any(p => p.Name == "ReleaseDate"));
	}
}

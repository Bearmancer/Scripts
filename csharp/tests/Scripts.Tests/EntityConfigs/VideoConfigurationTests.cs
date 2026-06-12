using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class VideoConfigurationTests
{
	[Test]
	public async Task Video_UploadDate_ColumnType_IsDate()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));
		var prop = entityType!.FindProperty("UploadDate");

		await Assert.That(prop).IsNotNull();
		await Assert
			.That(
				prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value
			)
			.IsEqualTo("date");
	}

	[Test]
	public async Task Video_SyncedAt_ColumnType_IsTimestamptz()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));
		var prop = entityType!.FindProperty("SyncedAt");

		await Assert.That(prop).IsNotNull();
		await Assert
			.That(
				prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value
			)
			.IsEqualTo("timestamptz");
	}

	[Test]
	public async Task Video_HasTitle_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));
		var indexes = entityType!.GetIndexes().ToList();

		await Assert.That(indexes).Contains(i => i.Properties.Any(p => p.Name == "Title"));
	}
}

using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class SourceRecordConfigurationTests
{
	[Test]
	public async Task SourceRecord_HasCorrectTableName()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(SourceRecord));

		await Assert.That(entityType).IsNotNull();
		await Assert.That(entityType!.GetTableName()).IsEqualTo("source_records");
	}

	[Test]
	public async Task SourceRecord_HasCompositeUniqueIndex_OnSourceIdAndEntityType()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(SourceRecord));

		await Assert.That(entityType).IsNotNull();
		var indexes = entityType!.GetIndexes().ToList();
		await Assert.That(indexes).Contains(i => i.Properties.Any(p => p.Name == "SourceId"));
		await Assert.That(indexes).Contains(i => i.Properties.Any(p => p.Name == "EntityType"));
	}

	[Test]
	public async Task SourceRecord_RawData_IsJsonb()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(SourceRecord));
		var rawDataProp = entityType!.FindProperty("RawData");

		await Assert.That(rawDataProp).IsNotNull();
		await Assert
			.That(
				rawDataProp!
					.GetAnnotations()
					.FirstOrDefault(a => a.Name == "Relational:ColumnType")
					?.Value
			)
			.IsEqualTo("jsonb");
	}
}

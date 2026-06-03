using TUnit;
using FluentAssertions;
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

		entityType.Should().NotBeNull();
		entityType!.GetTableName().Should().Be("source_records");
	}

	[Test]
	public async Task SourceRecord_HasCompositeUniqueIndex_OnSourceIdAndEntityType()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(SourceRecord));

		entityType.Should().NotBeNull();
		var indexes = entityType!.GetIndexes().ToList();
		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "SourceId"));
		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "EntityType"));
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

		rawDataProp.Should().NotBeNull();
		rawDataProp!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("jsonb");
	}
}

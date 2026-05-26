using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class FiberyEntityConfigurationTests
{
	[Test]
	public async Task FiberyEntity_HasCompositeUniqueIndex_OnFiberyIdAndEntityType()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(FiberyEntity));

		entityType.Should().NotBeNull();
		var indexes = entityType!.GetIndexes().ToList();
		indexes.Should().Contain(i =>
			i.Properties.Any(p => p.Name == "FiberyId") &&
			i.Properties.Any(p => p.Name == "EntityType") &&
			i.IsUnique);
	}

	[Test]
	public async Task FiberyEntity_HasEntityTypeIndex()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(FiberyEntity));

		var indexes = entityType!.GetIndexes().ToList();
		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "EntityType") && !i.IsUnique);
	}

	[Test]
	public async Task FiberyEntity_FiberyId_HasColumnType()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(FiberyEntity));
		var prop = entityType!.FindProperty("FiberyId");

		prop.Should().NotBeNull();
		prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("varchar(255)");
	}
}

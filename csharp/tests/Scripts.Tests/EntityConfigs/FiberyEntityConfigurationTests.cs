using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

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

		await Assert.That(entityType).IsNotNull();
		var indexes = entityType!.GetIndexes().ToList();
		await Assert
			.That(indexes)
			.Contains(i =>
				i.Properties.Any(p => p.Name == "FiberyId")
				&& i.Properties.Any(p => p.Name == "EntityType")
				&& i.IsUnique
			);
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
		await Assert
			.That(indexes)
			.Contains(i => i.Properties.Any(p => p.Name == "EntityType") && !i.IsUnique);
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

		await Assert.That(prop).IsNotNull();
		await Assert
			.That(
				prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value
			)
			.IsEqualTo("varchar(255)");
	}
}

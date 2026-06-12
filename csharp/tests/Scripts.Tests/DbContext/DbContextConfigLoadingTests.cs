using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.DbContext;

internal sealed class DbContextConfigLoadingTests
{
	[Test]
	public async Task OnModelCreating_Discovers_AllConfigEntities()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("ConfigDiscoveryTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var model = context.Model;

		var entityTypes = model.GetEntityTypes().Select(e => e.ClrType).ToList();

		await Assert.That(entityTypes).Contains(typeof(Artist));
		await Assert.That(entityTypes).Contains(typeof(Album));
		await Assert.That(entityTypes).Contains(typeof(Track));
		await Assert.That(entityTypes).Contains(typeof(Scrobble));
		await Assert.That(entityTypes).Contains(typeof(Video));
		await Assert.That(entityTypes).Contains(typeof(ExecutionLog));
		await Assert.That(entityTypes).Contains(typeof(FailedTask));
		await Assert.That(entityTypes).Contains(typeof(FiberyEntity));
	}

	[Test]
	public async Task ArtistsTable_HasCorrectName()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("TableNameTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Artist));

		await Assert.That(entityType).IsNotNull();
		await Assert.That(entityType!.GetTableName()).IsEqualTo("artists");
	}

	[Test]
	public async Task ScrobblesTable_HasCorrectTimestampColumnType()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("ColumnTypeTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);

		var entityType = context
			.Model.GetEntityTypes()
			.FirstOrDefault(e => e.ClrType == typeof(Scrobble));
		var scrobbledAt = entityType?.FindProperty("ScrobbledAt");

		await Assert.That(scrobbledAt).IsNotNull();
		await Assert
			.That(
				scrobbledAt!
					.GetAnnotations()
					.FirstOrDefault(a => a.Name == "Relational:ColumnType")
					?.Value
			)
			.IsEqualTo("timestamptz");
	}
}

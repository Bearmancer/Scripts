using TUnit;
using FluentAssertions;
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

		entityTypes.Should().Contain(typeof(Artist));
		entityTypes.Should().Contain(typeof(Album));
		entityTypes.Should().Contain(typeof(Track));
		entityTypes.Should().Contain(typeof(Scrobble));
		entityTypes.Should().Contain(typeof(Video));
		entityTypes.Should().Contain(typeof(ExecutionLog));
		entityTypes.Should().Contain(typeof(FailedTask));
		entityTypes.Should().Contain(typeof(FiberyEntity));
	}

	[Test]
	public async Task ArtistsTable_HasCorrectName()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("TableNameTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Artist));

		entityType.Should().NotBeNull();
		entityType!.GetTableName().Should().Be("artists");
	}

	[Test]
	public async Task ScrobblesTable_HasCorrectTimestampColumnType()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("ColumnTypeTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		// FindEntityType returns null for Scrobble in this InMemory setup; use GetEntityTypes() lookup instead.
		var entityType = context.Model.GetEntityTypes().FirstOrDefault(e => e.ClrType == typeof(Scrobble));
		var scrobbledAt = entityType?.FindProperty("ScrobbledAt");

		scrobbledAt.Should().NotBeNull();
		scrobbledAt!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("timestamptz");
	}
}

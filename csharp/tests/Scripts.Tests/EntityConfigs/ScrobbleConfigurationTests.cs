using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class ScrobbleConfigurationTests
{
	[Test]
	public async Task Scrobble_Platform_ColumnType_IsVarchar()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		
		var entityType = context.Model.GetEntityTypes().FirstOrDefault(e => e.ClrType == typeof(Scrobble));
		var prop = entityType?.FindProperty("Platform");

		prop.Should().NotBeNull();
		prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("varchar(50)");
	}

	[Test]
	public async Task Scrobble_HasPlatform_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.GetEntityTypes().FirstOrDefault(e => e.ClrType == typeof(Scrobble));
		var indexes = entityType!.GetIndexes().ToList();

		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Platform"));
	}

	[Test]
	public async Task Scrobble_HasStandaloneScrobbledAt_Index()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.GetEntityTypes().FirstOrDefault(e => e.ClrType == typeof(Scrobble));
		var indexes = entityType!.GetIndexes().ToList();

		indexes.Should().Contain(i =>
			i.Properties.Count == 1 &&
			i.Properties.Any(p => p.Name == "ScrobbledAt") &&
			!i.IsUnique);
	}
}

using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

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

		prop.Should().NotBeNull();
		prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("date");
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

		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "ReleaseDate"));
	}
}

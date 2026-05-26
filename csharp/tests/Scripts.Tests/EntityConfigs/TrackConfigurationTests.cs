using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

internal sealed class TrackConfigurationTests
{
	[Test]
	public async Task Track_Duration_ColumnType_IsInteger()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Track));
		var prop = entityType!.FindProperty("DurationSeconds");

		prop.Should().NotBeNull();
		prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("integer");
	}

	[Test]
	public async Task Track_HasCompositeUnique_OnArtistIdAndTitle()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Track));
		var indexes = entityType!.GetIndexes().ToList();

		indexes.Should().Contain(i =>
			i.Properties.Any(p => p.Name == "ArtistId") &&
			i.Properties.Any(p => p.Name == "Title") &&
			i.IsUnique);
	}
}

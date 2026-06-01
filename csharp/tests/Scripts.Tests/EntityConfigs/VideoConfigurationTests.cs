using TUnit;
using FluentAssertions;
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

		prop.Should().NotBeNull();
		prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("date");
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

		prop.Should().NotBeNull();
		prop!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("timestamptz");
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

		indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Title"));
	}
}

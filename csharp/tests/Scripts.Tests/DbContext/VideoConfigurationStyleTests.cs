using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.DbContext;

internal sealed class VideoConfigurationStyleTests
{
	[Test]
	public async Task VideoConfiguration_StillHas_UrlUniqueIndex_AfterStaticFix()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("VideoStyleTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));

		entityType.Should().NotBeNull();
		var urlProperty = entityType!.FindProperty("Url");
		urlProperty.Should().NotBeNull();
		urlProperty!.IsNullable.Should().BeFalse();
	}

	[Test]
	public async Task VideoConfiguration_StillHas_MetadataJsonbType_AfterStaticFix()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("VideoMetaTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));
		var metadataProp = entityType!.FindProperty("Metadata");

		metadataProp.Should().NotBeNull();
		metadataProp!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("jsonb");
	}
}

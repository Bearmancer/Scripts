using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Data.Entities;

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

		await Assert.That(entityType).IsNotNull();
		var urlProperty = entityType!.FindProperty("Url");
		await Assert.That(urlProperty).IsNotNull();
		await Assert.That(urlProperty!.IsNullable).IsFalse();
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

		await Assert.That(metadataProp).IsNotNull();
		await Assert
			.That(
				metadataProp!
					.GetAnnotations()
					.FirstOrDefault(a => a.Name == "Relational:ColumnType")
					?.Value
			)
			.IsEqualTo("jsonb");
	}
}

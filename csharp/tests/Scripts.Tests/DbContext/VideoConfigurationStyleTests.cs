<<<<<<< HEAD
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;
=======
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1

namespace Scripts.Tests.DbContext;

internal sealed class VideoConfigurationStyleTests
{
	[Test]
	public async Task VideoConfiguration_StillHas_UrlUniqueIndex_AfterStaticFix()
	{
<<<<<<< HEAD
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var entityType = context.Model.FindEntityType(typeof(Video));

				entityType.Should().NotBeNull();
				var urlProperty = entityType!.FindProperty("Url");
				urlProperty.Should().NotBeNull();
				urlProperty!.IsNullable.Should().BeFalse();
			}
		}
=======
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("VideoStyleTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));

		entityType.Should().NotBeNull();
		var urlProperty = entityType!.FindProperty("Url");
		urlProperty.Should().NotBeNull();
		urlProperty!.IsNullable.Should().BeFalse();
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1
	}

	[Test]
	public async Task VideoConfiguration_StillHas_MetadataJsonbType_AfterStaticFix()
	{
<<<<<<< HEAD
		var fixture = new DatabaseTestFixture();
		await fixture.InitializeAsync();
		await using (fixture)
		{
			var context = fixture.GetContext();
			await using (context)
			{
				var entityType = context.Model.FindEntityType(typeof(Video));
				var metadataProp = entityType!.FindProperty("Metadata");

				metadataProp.Should().NotBeNull();
				metadataProp!
					.GetAnnotations()
					.FirstOrDefault(a => a.Name == "Relational:ColumnType")
					?.Value.Should()
					.Be("jsonb");
			}
		}
=======
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase("VideoMetaTest_" + Guid.NewGuid())
			.Options;

		await using var context = new ScriptsDbContext(options);
		var entityType = context.Model.FindEntityType(typeof(Video));
		var metadataProp = entityType!.FindProperty("Metadata");

		metadataProp.Should().NotBeNull();
		metadataProp!.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value.Should().Be("jsonb");
>>>>>>> d057b9bb8ac223cfc175063f75aa77cad063fcb1
	}
}

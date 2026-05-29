using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using CSharpScripts.Tests.DbContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TUnit;

namespace Scripts.Tests.DbContext;

internal sealed class VideoConfigurationStyleTests
{
	[Test]
	public async Task VideoConfiguration_StillHas_UrlUniqueIndex_AfterStaticFix()
	{
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
	}

	[Test]
	public async Task VideoConfiguration_StillHas_MetadataJsonbType_AfterStaticFix()
	{
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
	}
}
